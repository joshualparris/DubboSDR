using System;
using System.Threading.Tasks;
using NAudio.Wave;

namespace DubboSDR.App.Services.Streaming
{
    public class InternetAudioSource : IAudioSource
    {
        public string SourceType => "Internet";
        
        public bool IsPlaying => _waveOut?.PlaybackState == PlaybackState.Playing;

        private WaveOutEvent? _waveOut;
        private MediaFoundationReader? _reader;
        private float _volume = 1.0f;

        public Task<bool> StartAsync(string? uriOrFrequency = null)
        {
            if (string.IsNullOrEmpty(uriOrFrequency))
                return Task.FromResult(false);

            try
            {
                StopInternal();

                _reader = new MediaFoundationReader(uriOrFrequency);
                _waveOut = new WaveOutEvent();
                _waveOut.Init(_reader);
                _waveOut.Volume = _volume;
                _waveOut.Play();
                
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"InternetAudioSource Error: {ex.Message}");
                StopInternal();
                return Task.FromResult(false);
            }
        }

        public Task StopAsync()
        {
            StopInternal();
            return Task.CompletedTask;
        }

        private void StopInternal()
        {
            if (_waveOut != null)
            {
                _waveOut.Stop();
                _waveOut.Dispose();
                _waveOut = null;
            }
            if (_reader != null)
            {
                _reader.Dispose();
                _reader = null;
            }
        }

        public void SetVolume(float volume)
        {
            _volume = Math.Clamp(volume, 0f, 1f);
            if (_waveOut != null)
            {
                _waveOut.Volume = _volume;
            }
        }

        public void Dispose()
        {
            StopInternal();
        }
    }
}
