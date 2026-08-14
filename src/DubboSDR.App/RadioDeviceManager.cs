using System;
using System.Threading;
using System.Threading.Tasks;
using DubboSDR.Core;
using DubboSDR.RtlSdr.Native;

namespace DubboSDR.App
{
    public class RadioDeviceManager : IDisposable
    {
        private IntPtr _dev = IntPtr.Zero;
        private CancellationTokenSource? _cts;
        private Task? _readTask;
        private readonly SemaphoreSlim _tuneLock = new SemaphoreSlim(1, 1);

        private FmDemodulator _demodulator;
        private AudioPlayer _audioPlayer;

        public event Action<double>? OnSignalStrengthUpdated;
        public event Action<string>? OnError;
        public event Action<string>? OnDebugInfoUpdated;
        
        public double LastSignalStrength { get; private set; }

        private uint _currentFreqHz = 93500000;
        private const uint SDR_SAMPLE_RATE = 960000; // 960 kS/s
        private const int BUFFER_SIZE = 128 * 1024; // 128 KB blocks

        private int[] _supportedGains = new int[] { 0 };
        private int _currentGainIdx = -1;

        public RadioDeviceManager(FmDemodulator demodulator, AudioPlayer audioPlayer)
        {
            _demodulator = demodulator;
            _audioPlayer = audioPlayer;
        }

        public bool Connect()
        {
            if (_dev != IntPtr.Zero) return true;

            uint count = RtlSdrNative.rtlsdr_get_device_count();
            if (count == 0)
            {
                OnError?.Invoke("No RTL-SDR devices found.");
                return false;
            }

            int res = RtlSdrNative.rtlsdr_open(out _dev, 0);
            if (res < 0)
            {
                OnError?.Invoke("NESDR is unavailable. SDR++ may currently be using it.");
                _dev = IntPtr.Zero;
                return false;
            }

            RtlSdrNative.rtlsdr_set_sample_rate(_dev, SDR_SAMPLE_RATE);
            
            // Get supported gains
            int[] tempGains = new int[100];
            int numGains = RtlSdrNative.rtlsdr_get_tuner_gains(_dev, tempGains);
            if (numGains > 0)
            {
                _supportedGains = new int[numGains];
                Array.Copy(tempGains, _supportedGains, numGains);
                Array.Sort(_supportedGains);
            }
            else
            {
                // Fallback
                _supportedGains = new int[] { 0, 100, 200, 300, 400 };
            }

            RtlSdrNative.rtlsdr_set_tuner_gain_mode(_dev, 1); 
            
            // Set moderate gain initially (around middle of array)
            _currentGainIdx = _supportedGains.Length / 2;
            RtlSdrNative.rtlsdr_set_tuner_gain(_dev, _supportedGains[_currentGainIdx]);

            return true;
        }

        public void Disconnect()
        {
            StopStreaming();
            if (_dev != IntPtr.Zero)
            {
                RtlSdrNative.rtlsdr_close(_dev);
                _dev = IntPtr.Zero;
            }
        }

        public async Task<bool> TuneAsync(uint frequencyHz, int tuneId, CancellationToken ct)
        {
            if (_dev == IntPtr.Zero) return false;

            await _tuneLock.WaitAsync(ct);
            try
            {
                if (ct.IsCancellationRequested) return false;

                Console.WriteLine($"[Tune {tuneId}] Read loop stopping");
                bool wasReading = (_readTask != null);
                StopStreaming();
                Console.WriteLine($"[Tune {tuneId}] Read loop stopped");

                Console.WriteLine($"[Tune {tuneId}] Audio buffer cleared");
                _audioPlayer.ClearBuffer();
                
                Console.WriteLine($"[Tune {tuneId}] DSP reset");
                _demodulator.Reset();
                
                _currentFreqHz = frequencyHz;
                // +240 kHz Offset Tuning
                uint hwFreq = _currentFreqHz - 240000;
                int res = RtlSdrNative.rtlsdr_set_center_freq(_dev, hwFreq);
                Console.WriteLine($"[Tune {tuneId}] Hardware tuned: {hwFreq/1000000.0:F3} MHz");
                
                if (res < 0) return false;

                RtlSdrNative.rtlsdr_reset_buffer(_dev);
                Console.WriteLine($"[Tune {tuneId}] RTL buffer reset: PASS");

                // We must start reading again so buffers fill
                Console.WriteLine($"[Tune {tuneId}] Read loop started");
                StartStreaming();
                
                // Wait until we actually have audio
                Console.WriteLine($"[Tune {tuneId}] Waiting for audio buffer...");
                int waitTimeout = 2000; // 2 seconds max
                while (_audioPlayer.BufferedDuration <= TimeSpan.Zero)
                {
                    await Task.Delay(20, ct);
                    waitTimeout -= 20;
                    if (waitTimeout <= 0 || ct.IsCancellationRequested) return false;
                }
                
                Console.WriteLine($"[Tune {tuneId}] First audio samples queued");
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            finally
            {
                _tuneLock.Release();
            }
        }

        public void StartStreaming()
        {
            if (_dev == IntPtr.Zero || _readTask != null) return;

            _cts = new CancellationTokenSource();
            _readTask = Task.Run(() => ReadLoop(_cts.Token));
        }

        public void StopStreaming()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _readTask?.Wait(2000);
                _cts.Dispose();
                _cts = null;
                _readTask = null;
            }
        }

        private void ReadLoop(CancellationToken token)
        {
            byte[] rawBuffer = new byte[BUFFER_SIZE];
            long totalBuffers = 0;
            DateTime lastDebugUpdate = DateTime.Now;

            while (!token.IsCancellationRequested)
            {
                int n_read = 0;
                int res = 0;
                
                unsafe
                {
                    fixed (byte* p = rawBuffer)
                    {
                        res = RtlSdrNative.rtlsdr_read_sync(_dev, (IntPtr)p, BUFFER_SIZE, out n_read);
                    }
                }

                if (res < 0 || token.IsCancellationRequested)
                    break;

                if (n_read > 0)
                {
                    if (totalBuffers == 0)
                    {
                        Console.WriteLine($"First IQ buffer received (size {n_read})");
                    }
                    
                    // Skip first 2 buffers to let SDR front-end settle after a retune
                    totalBuffers++;
                    if (totalBuffers < 3) continue;

                    // Analyze IQ correctly
                    int clipCount = 0;
                    double sumSq = 0;
                    int checkSamples = Math.Min(n_read, 4096);
                    
                    for(int i = 0; i < checkSamples; i++)
                    {
                        byte b = rawBuffer[i];
                        if (b <= 3 || b >= 252) clipCount++;
                        if (i % 2 == 0)
                        {
                            float I = (rawBuffer[i] - 127.5f) / 127.5f;
                            float Q = (rawBuffer[i+1] - 127.5f) / 127.5f;
                            sumSq += (I * I + Q * Q);
                        }
                    }
                    
                    double rms = Math.Sqrt(sumSq / (checkSamples / 2.0));
                    double clipPercent = (clipCount / (double)checkSamples) * 100.0;
                    
                    // Signal Strength 0-100 derived from 0.0 to 0.5 RMS typical range
                    double displaySignal = Math.Min(100, (rms / 0.3) * 100);
                    LastSignalStrength = displaySignal;
                    OnSignalStrengthUpdated?.Invoke(displaySignal);

                    // Auto AGC Algorithm using supported gains
                    if (clipPercent > 0.1 && _currentGainIdx > 0) 
                    {
                        _currentGainIdx--;
                        RtlSdrNative.rtlsdr_set_tuner_gain(_dev, _supportedGains[_currentGainIdx]);
                    }
                    else if (rms < 0.05 && clipPercent == 0 && _currentGainIdx < _supportedGains.Length - 1)
                    {
                        _currentGainIdx++;
                        RtlSdrNative.rtlsdr_set_tuner_gain(_dev, _supportedGains[_currentGainIdx]);
                    }

                    // Process DSP
                    float[] audioSamples = _demodulator.Process(rawBuffer, n_read);
                    _audioPlayer.AddSamples(audioSamples);

                    // Debug Panel update
                    if ((DateTime.Now - lastDebugUpdate).TotalSeconds >= 0.5)
                    {
                        int gain = RtlSdrNative.rtlsdr_get_tuner_gain(_dev);
                        string dbg = $"RF Gain: {gain/10.0:F1} dB | ADC Clip: {clipPercent:F1}% | IQ RMS: {rms:F4}";
                        OnDebugInfoUpdated?.Invoke(dbg);
                        lastDebugUpdate = DateTime.Now;
                    }
                }
            }
        }

        public void Dispose()
        {
            Disconnect();
            _tuneLock.Dispose();
        }
    }
}
