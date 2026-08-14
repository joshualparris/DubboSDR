using System;
using System.Threading;
using System.Threading.Tasks;
using DubboSDR.Core;

namespace DubboSDR.App.Services
{
    public class RadioService : IDisposable
    {
        public RadioDeviceManager DeviceManager { get; }
        public FmDemodulator Demodulator { get; }
        public AudioPlayer AudioPlayer { get; }

        public event Action<Station>? OnStationChanged;

        private Station? _currentStation;
        private CancellationTokenSource? _tuneCts;
        private int _tuneRequestId = 0;

        public RadioService()
        {
            Demodulator = new FmDemodulator();
            AudioPlayer = new AudioPlayer(Demodulator.OutputSampleRate);
            DeviceManager = new RadioDeviceManager(Demodulator, AudioPlayer);
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

            // Handle Internet Streams (Mocked for now)
            if (station.Category == "InternetAudio" || station.Mode == "Internet")
            {
                DeviceManager.StopStreaming();
                AudioPlayer.Stop();
                // TODO: Actually play internet stream. For now, it will just be silent.
                OnStationChanged?.Invoke(station);
                return true;
            }

            Console.WriteLine($"Tune request {reqId}: {station.FrequencyHz}");

            // Handle SDR
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
        }

        public void Pause()
        {
            DeviceManager.StopStreaming();
            AudioPlayer.Stop();
        }

        public void Resume()
        {
            if (_currentStation != null && _currentStation.Mode != "Internet")
            {
                DeviceManager.StartStreaming();
                AudioPlayer.Play();
            }
        }

        public void Dispose()
        {
            _tuneCts?.Cancel();
            DeviceManager?.Dispose();
            AudioPlayer?.Dispose();
        }
    }
}
