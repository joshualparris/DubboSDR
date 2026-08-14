using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Lame;

namespace DubboSDR.App.Services.Streaming
{
    public interface IRemoteAudioEncoder
    {
        string ContentType { get; }
        Task StreamAudioAsync(ClientAudioQueue queue, Stream outputStream, CancellationToken ct);
    }

    public class Mp3StreamEncoder : IRemoteAudioEncoder
    {
        public string ContentType => "audio/mpeg";

        public async Task StreamAudioAsync(ClientAudioQueue queue, Stream outputStream, CancellationToken ct)
        {
            var format = WaveFormat.CreateIeeeFloatWaveFormat(48000, 1);
            
            // LameMP3FileWriter expects a Stream to write to.
            // We use LAMEPreset.VBR_90 for roughly 64-96kbps which is perfect for speech/FM.
            using var encoder = new LameMP3FileWriter(outputStream, format, LAMEPreset.VBR_90);
            
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    float[] samples = await queue.ReadAsync(ct);
                    
                    int byteCount = samples.Length * 4;
                    byte[] bytes = new byte[byteCount];
                    Buffer.BlockCopy(samples, 0, bytes, 0, byteCount);
                    
                    // Write to MP3 encoder, which internally writes MP3 frames to outputStream
                    encoder.Write(bytes, 0, byteCount);
                    
                    // Periodically flush the underlying HTTP response stream so browsers receive chunks
                    await outputStream.FlushAsync(ct);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal disconnect
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Streaming client disconnected: {ex.Message}");
            }
        }
    }
}
