using System;
using System.Threading;
using System.Threading.Tasks;

namespace DubboSDR.App.Services.Streaming
{
    public class SdrAudioSource : IAudioSource
    {
        public string SourceType => "SDR";
        public bool IsPlaying { get; private set; }

        private readonly RadioDeviceManager _deviceManager;
        private readonly AudioPlayer _audioPlayer;

        public SdrAudioSource(RadioDeviceManager deviceManager, AudioPlayer audioPlayer)
        {
            _deviceManager = deviceManager;
            _audioPlayer = audioPlayer;
        }

        public async Task<bool> StartAsync(string? uriOrFrequency = null)
        {
            if (!uint.TryParse(uriOrFrequency, out var freqHz))
                return false;

            try
            {
                if (!_deviceManager.Connect())
                {
                    Console.WriteLine("SDR Hardware not found or failed to connect.");
                    return false;
                }

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                bool success = await _deviceManager.TuneAsync(freqHz, 0, cts.Token);
                
                if (success)
                {
                    _audioPlayer.Play();
                    IsPlaying = true;
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SdrAudioSource Error: {ex.Message}");
                return false;
            }
        }

        public Task StopAsync()
        {
            _deviceManager.StopStreaming();
            _audioPlayer.Stop();
            _deviceManager.Disconnect(); // Release hardware entirely when not in use
            IsPlaying = false;
            return Task.CompletedTask;
        }

        public void SetVolume(float volume)
        {
            _audioPlayer.SetVolume(volume);
        }

        public void Dispose()
        {
            StopAsync().Wait();
        }
    }
}
