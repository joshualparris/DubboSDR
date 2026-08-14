using System;
using NAudio.Wave;

namespace DubboSDR.App
{
    public class AudioPlayer : IDisposable
    {
        private WaveOutEvent _waveOut;
        private BufferedWaveProvider _waveProvider;

        public TimeSpan BufferedDuration => _waveProvider?.BufferedDuration ?? TimeSpan.Zero;

        public AudioPlayer(int sampleRate)
        {
            // Create a float wave format (mono)
            var format = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);
            
            _waveProvider = new BufferedWaveProvider(format)
            {
                BufferDuration = TimeSpan.FromSeconds(2),
                DiscardOnBufferOverflow = true
            };
            
            _waveOut = new WaveOutEvent
            {
                DesiredLatency = 150 // Keep latency low but stable
            };

            _waveOut.Init(_waveProvider);
            _waveOut.Volume = 1.0f; // Use hardware/mixer volume instead of sample manipulation
            _waveOut.Play();
        }

        public void Play()
        {
            if (_waveOut.PlaybackState != PlaybackState.Playing)
            {
                _waveOut.Play();
            }
        }

        public void Stop()
        {
            _waveOut.Stop();
        }

        public void ClearBuffer()
        {
            _waveProvider.ClearBuffer();
        }

        public void AddSamples(float[] samples)
        {
            // Convert float[] to byte[] for NAudio
            int byteCount = samples.Length * 4;
            byte[] bytes = new byte[byteCount];
            Buffer.BlockCopy(samples, 0, bytes, 0, byteCount);
            
            // Add to buffer
            _waveProvider.AddSamples(bytes, 0, byteCount);
        }

        public void SetVolume(float vol)
        {
            _waveOut.Volume = Math.Clamp(vol, 0f, 1f);
        }

        public void Dispose()
        {
            _waveOut?.Stop();
            _waveOut?.Dispose();
        }
    }
}
