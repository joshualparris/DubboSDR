using System;
using System.Threading;
using System.Threading.Tasks;
using DubboSDR.Core;
using DubboSDR.App.Services.Streaming;

namespace DubboSDR.App.Services
{
    public class RadioService : IDisposable
    {
        public RadioDeviceManager DeviceManager { get; }
        public FmDemodulator Demodulator { get; }
        public AudioPlayer AudioPlayer { get; }
        public AudioBroadcaster Broadcaster { get; }

        public event Action<Station>? OnStationChanged;

        private Station? _currentStation;
        public Station? CurrentStation => _currentStation;

        private IAudioSource _internetSource;
        private IAudioSource _sdrSource;
        private IAudioSource? _activeSource;

        private float _volume = 1.0f;

        public RadioService()
        {
            Demodulator = new FmDemodulator();
            Broadcaster = new AudioBroadcaster();
            AudioPlayer = new AudioPlayer(Demodulator.OutputSampleRate, Broadcaster);
            DeviceManager = new RadioDeviceManager(Demodulator, AudioPlayer);
            
            _internetSource = new InternetAudioSource();
            _sdrSource = new SdrAudioSource(DeviceManager, AudioPlayer);
        }

        public bool Connect()
        {
            // Connect is mostly deferred to when SDR is requested, but we can allow pre-connect if needed
            return true; 
        }

        public void Disconnect()
        {
            _sdrSource.StopAsync().Wait();
        }

        public async Task<bool> TuneAsync(Station station)
        {
            _currentStation = station;
            
            if (_activeSource != null)
            {
                await _activeSource.StopAsync();
                _activeSource = null;
            }

            // Handle Internet Streams (Internet or Hybrid default)
            if (station.SourceType == "Internet" || station.SourceType == "Hybrid")
            {
                if (!string.IsNullOrEmpty(station.StreamUrl))
                {
                    bool success = await _internetSource.StartAsync(station.StreamUrl);
                    if (success)
                    {
                        _activeSource = _internetSource;
                        _activeSource.SetVolume(_volume);
                        OnStationChanged?.Invoke(station);
                        return true;
                    }
                    else if (station.SourceType == "Hybrid")
                    {
                        Console.WriteLine("Internet stream failed. Falling back to SDR.");
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            // Handle SDR (Live RF Explorer or manual fallback)
            bool sdrSuccess = await _sdrSource.StartAsync(station.FrequencyHz.ToString());
            if (sdrSuccess)
            {
                _activeSource = _sdrSource;
                _activeSource.SetVolume(_volume);
                OnStationChanged?.Invoke(station);
                return true;
            }
            
            return false;
        }

        public void SetVolume(float volume)
        {
            _volume = volume;
            _activeSource?.SetVolume(volume);
        }

        public void Pause()
        {
            _activeSource?.StopAsync();
        }

        public void Resume()
        {
            if (_currentStation != null && _activeSource != null)
            {
                if (_activeSource == _internetSource)
                {
                    _internetSource.StartAsync(_currentStation.StreamUrl);
                }
                else
                {
                    _sdrSource.StartAsync(_currentStation.FrequencyHz.ToString());
                }
            }
        }

        public void Dispose()
        {
            _internetSource?.Dispose();
            _sdrSource?.Dispose();
        }
    }
}
