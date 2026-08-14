using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
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

        private CancellationTokenSource? _tuneCts;
        private int _tuneRequestId = 0;
        private MediaPlayer _internetPlayer;

        public RadioService()
        {
            Demodulator = new FmDemodulator();
            Broadcaster = new AudioBroadcaster();
            AudioPlayer = new AudioPlayer(Demodulator.OutputSampleRate, Broadcaster);
            DeviceManager = new RadioDeviceManager(Demodulator, AudioPlayer);
            
            // Internet stream player (WPF Native)
            _internetPlayer = new MediaPlayer();
            _internetPlayer.Volume = 1.0;
        }

        public bool Connect()
        {
            return DeviceManager.Connect();
        }

        public void Disconnect()
        {
            DeviceManager.Disconnect();
        }

        public async Task<bool> TuneAsync(Station station)
        {
            _currentStation = station;
            
            _tuneCts?.Cancel();
            _tuneCts = new CancellationTokenSource();
            var ct = _tuneCts.Token;
            int reqId = Interlocked.Increment(ref _tuneRequestId);

            // Handle Internet Streams (Internet or Hybrid default)
            if (station.SourceType == "Internet" || station.SourceType == "Hybrid")
            {
                DeviceManager.StopStreaming();
                AudioPlayer.Stop();
                
                _internetPlayer.Stop();
                if (!string.IsNullOrEmpty(station.StreamUrl))
                {
                    _internetPlayer.Open(new Uri(station.StreamUrl));
                    _internetPlayer.Play();
                }

                OnStationChanged?.Invoke(station);
                return true;
            }

            // Handle SDR (Live RF Explorer or manual fallback)
            _internetPlayer.Stop(); // Ensure internet stream stops when SDR starts
            Console.WriteLine($"Tune request {reqId}: {station.FrequencyHz}");

            bool success = await DeviceManager.TuneAsync(station.FrequencyHz, reqId, ct);
            
            if (success && !ct.IsCancellationRequested)
            {
                AudioPlayer.Play();
                OnStationChanged?.Invoke(station);
                Console.WriteLine($"Tune {reqId} COMPLETE\n");
                return true;
            }
            
            return false;
        }

        public void SetVolume(float volume)
        {
            AudioPlayer.SetVolume(volume);
            _internetPlayer.Volume = Math.Clamp(volume, 0f, 1f);
        }

        public void Pause()
        {
            DeviceManager.StopStreaming();
            AudioPlayer.Stop();
            _internetPlayer.Pause();
        }

        public void Resume()
        {
            if (_currentStation != null)
            {
                if (_currentStation.SourceType == "Internet" || _currentStation.SourceType == "Hybrid")
                {
                    _internetPlayer.Play();
                }
                else
                {
                    DeviceManager.StartStreaming();
                    AudioPlayer.Play();
                }
            }
        }

        public void Dispose()
        {
            _tuneCts?.Cancel();
            DeviceManager?.Dispose();
            AudioPlayer?.Dispose();
            _internetPlayer?.Close();
        }
    }
}
