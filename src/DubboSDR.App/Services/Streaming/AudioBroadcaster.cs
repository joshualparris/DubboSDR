using System;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace DubboSDR.App.Services.Streaming
{
    public class ClientAudioQueue : IDisposable
    {
        private readonly Channel<float[]> _channel;
        public Guid Id { get; } = Guid.NewGuid();

        public ClientAudioQueue()
        {
            // Bounded channel to prevent memory leaks if a client disconnects badly or falls behind.
            // 50 buffers of say 2048 samples is about 100k samples (~2 seconds of audio at 48kHz).
            _channel = Channel.CreateBounded<float[]>(new BoundedChannelOptions(50)
            {
                FullMode = BoundedChannelFullMode.DropOldest
            });
        }

        public void Enqueue(float[] samples)
        {
            // We must copy the array because the demodulator reuses or overwrites the original, 
            // or the original might change. Actually, FmDemodulator creates a new float[] every time, 
            // so we can just pass the reference to avoid GC pressure!
            _channel.Writer.TryWrite(samples);
        }

        public async System.Threading.Tasks.Task<float[]> ReadAsync(System.Threading.CancellationToken ct)
        {
            return await _channel.Reader.ReadAsync(ct);
        }

        public void Dispose()
        {
            _channel.Writer.TryComplete();
        }
    }

    public class AudioBroadcaster
    {
        private readonly ConcurrentDictionary<Guid, ClientAudioQueue> _clients = new();

        public ClientAudioQueue Subscribe()
        {
            var queue = new ClientAudioQueue();
            _clients.TryAdd(queue.Id, queue);
            return queue;
        }

        public void Unsubscribe(Guid id)
        {
            if (_clients.TryRemove(id, out var queue))
            {
                queue.Dispose();
            }
        }

        public void Broadcast(float[] samples)
        {
            if (_clients.IsEmpty) return;

            foreach (var client in _clients.Values)
            {
                client.Enqueue(samples);
            }
        }

        public int ListenerCount => _clients.Count;
    }
}
