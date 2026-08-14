using System;
using System.IO;
using System.Runtime.InteropServices;
using DubboSDR.RtlSdr.Native;
using DubboSDR.Core;
using NAudio.Wave;

namespace DubboSDR.Diagnostics
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--scan")
            {
                RunScan();
                return;
            }
            if (args.Length > 0 && args[0] == "--sweep")
            {
                RunGainSweep(93500000);
                RunGainSweep(105500000);
                return;
            }

            uint freq = 93500000;
            if (args.Length > 0 && uint.TryParse(args[0], out uint f))
            {
                freq = f;
            }

            RunDiagnostic(freq);
        }

        static void RunDiagnostic(uint targetFreqHz)
        {
            Console.WriteLine($"\n=== Testing {targetFreqHz / 1000000.0:F3} MHz ===");
            int res = RtlSdrNative.rtlsdr_open(out IntPtr dev, 0);
            if (res < 0) return;

            try
            {
                uint offset = 240000; 
                uint hwFreq = targetFreqHz - offset;
                uint rate = 960000; 
                
                RtlSdrNative.rtlsdr_set_center_freq(dev, hwFreq);
                RtlSdrNative.rtlsdr_set_sample_rate(dev, rate);
                RtlSdrNative.rtlsdr_set_tuner_gain_mode(dev, 0); // auto
                RtlSdrNative.rtlsdr_reset_buffer(dev);

                int chunkLen = 256 * 1024;
                byte[] buffer = new byte[chunkLen];
                
                FmDemodulator demod = new FmDemodulator();
                
                string outDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "diagnostics");
                Directory.CreateDirectory(outDir);
                string outFile = Path.Combine(outDir, $"{targetFreqHz / 1000000.0:F3}MHz-cleaned.wav");
                
                var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(demod.OutputSampleRate, 1);
                
                double sumSqRF = 0;
                long totalSamplesRF = 0;
                int clipCount = 0;
                
                double sumSqAudio = 0;
                long totalSamplesAudio = 0;
                int clippedAudioCount = 0;
                float peakAudio = 0;
                
                using (var writer = new WaveFileWriter(outFile, waveFormat))
                {
                    int chunksToRead = 19200000 / chunkLen; // 10 seconds
                    
                    for (int c = 0; c < chunksToRead; c++)
                    {
                        int n_read = 0;
                        unsafe
                        {
                            fixed (byte* p = buffer)
                            {
                                RtlSdrNative.rtlsdr_read_sync(dev, (IntPtr)p, chunkLen, out n_read);
                            }
                        }

                        if (n_read == 0) continue;

                        for (int i = 0; i < n_read; i++)
                        {
                            byte b = buffer[i];
                            if (b <= 3 || b >= 252) clipCount++;
                            if (i % 2 == 0)
                            {
                                float I = (buffer[i] - 127.5f) / 127.5f;
                                float Q = (buffer[i+1] - 127.5f) / 127.5f;
                                sumSqRF += (I * I + Q * Q);
                                totalSamplesRF++;
                            }
                        }

                        float[] audio = demod.Process(buffer, n_read);
                        
                        foreach (var a in audio)
                        {
                            float absA = Math.Abs(a);
                            if (absA > peakAudio) peakAudio = absA;
                            if (absA >= 0.99f) clippedAudioCount++;
                            sumSqAudio += (a * a);
                            totalSamplesAudio++;
                        }
                        
                        int byteCount = audio.Length * 4;
                        byte[] audioBytes = new byte[byteCount];
                        Buffer.BlockCopy(audio, 0, audioBytes, 0, byteCount);
                        writer.Write(audioBytes, 0, byteCount);
                    }
                }

                Console.WriteLine($"ADC overload (Rails): {(double)clipCount / (totalSamplesRF*2) * 100.0:F2}%");
                Console.WriteLine($"IQ RMS: {Math.Sqrt(sumSqRF / totalSamplesRF):F4}");
                
                Console.WriteLine($"Demodulated WAV: {outFile}");
                Console.WriteLine($"Audio RMS: {Math.Sqrt(sumSqAudio / totalSamplesAudio):F4}");
                Console.WriteLine($"Peak amplitude: {peakAudio:F4}");
                Console.WriteLine($"Audio clipping: {(double)clippedAudioCount / totalSamplesAudio * 100.0:F2}%");
            }
            finally
            {
                RtlSdrNative.rtlsdr_close(dev);
            }
        }

        static void RunGainSweep(uint targetFreqHz)
        {
            Console.WriteLine($"\n=== Gain Sweep on {targetFreqHz / 1000000.0:F3} MHz ===");
            int res = RtlSdrNative.rtlsdr_open(out IntPtr dev, 0);
            if (res < 0) return;

            try
            {
                int[] tempGains = new int[100];
                int numGains = RtlSdrNative.rtlsdr_get_tuner_gains(dev, tempGains);
                int[] gains = new int[numGains];
                Array.Copy(tempGains, gains, numGains);
                Array.Sort(gains);

                RtlSdrNative.rtlsdr_set_sample_rate(dev, 960000);
                uint hwFreq = targetFreqHz - 240000;
                RtlSdrNative.rtlsdr_set_center_freq(dev, hwFreq);
                RtlSdrNative.rtlsdr_set_tuner_gain_mode(dev, 1);

                Console.WriteLine("Gain(dB) | IQ RMS  | Rails % | Audio RMS | Audio Clip %");

                int chunkLen = 256 * 1024;
                byte[] buffer = new byte[chunkLen];

                foreach (int gain in gains)
                {
                    RtlSdrNative.rtlsdr_set_tuner_gain(dev, gain);
                    System.Threading.Thread.Sleep(50); // let gain settle
                    RtlSdrNative.rtlsdr_reset_buffer(dev);
                    
                    FmDemodulator demod = new FmDemodulator();
                    
                    double sumSqRF = 0;
                    long totalSamplesRF = 0;
                    int clipCount = 0;
                    
                    double sumSqAudio = 0;
                    long totalSamplesAudio = 0;
                    int clippedAudioCount = 0;
                    
                    for (int c = 0; c < 10; c++) // ~2.5 seconds
                    {
                        int n_read = 0;
                        unsafe { fixed (byte* p = buffer) { RtlSdrNative.rtlsdr_read_sync(dev, (IntPtr)p, chunkLen, out n_read); } }
                        if (n_read == 0) continue;

                        for (int i = 0; i < n_read; i++)
                        {
                            byte b = buffer[i];
                            if (b <= 3 || b >= 252) clipCount++;
                            if (i % 2 == 0)
                            {
                                float I = (buffer[i] - 127.5f) / 127.5f;
                                float Q = (buffer[i+1] - 127.5f) / 127.5f;
                                sumSqRF += (I * I + Q * Q);
                                totalSamplesRF++;
                            }
                        }

                        float[] audio = demod.Process(buffer, n_read);
                        foreach (var a in audio)
                        {
                            if (Math.Abs(a) >= 0.99f) clippedAudioCount++;
                            sumSqAudio += (a * a);
                            totalSamplesAudio++;
                        }
                    }

                    double rails = (double)clipCount / (totalSamplesRF*2) * 100.0;
                    double audioClip = (double)clippedAudioCount / totalSamplesAudio * 100.0;
                    Console.WriteLine($"{gain/10.0,8:F1} | {Math.Sqrt(sumSqRF/totalSamplesRF),7:F4} | {rails,7:F2} | {Math.Sqrt(sumSqAudio/totalSamplesAudio),9:F4} | {audioClip,12:F2}");
                }
            }
            finally
            {
                RtlSdrNative.rtlsdr_close(dev);
            }
        }
        
        static void RunScan() {} // Omitted for brevity since we're focused on DSP hardening
    }
}
