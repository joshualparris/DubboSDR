using System;

namespace DubboSDR.Core
{
    public class FmDemodulator
    {
        private float _lastI = 0;
        private float _lastQ = 0;
        private float _deemphasisState = 0;
        private readonly float _deemphasisAlpha;
        private int _mixerPhase = 0;

        public const int Decimation1 = 4;
        public const int Decimation2 = 5;

        public int OutputSampleRate { get; } = 48000;

        private float[] _channelFir;
        private float[] _audioFir;

        private float[] _firStateI;
        private float[] _firStateQ;
        private int _firStateIdx = 0;
        
        private float[] _audioFirState;
        private int _audioFirStateIdx = 0;

        private readonly float _discriminatorGain;

        public FmDemodulator()
        {
            float fs1 = 960000f;
            float fs2 = fs1 / Decimation1; // 240,000 Hz
            float fsAudio = fs2 / Decimation2; // 48,000 Hz

            // 50 us de-emphasis at 48kHz
            float tau = 50e-6f;
            _deemphasisAlpha = (float)Math.Exp(-1.0 / (fsAudio * tau));

            // Discriminator scale: 75kHz deviation = 1.0 amplitude
            // Phase diff deltaPhi = 2pi * deltaF / Fs
            // To get normalized amplitude where 1.0 = 75kHz deviation:
            // amplitude = deltaPhi * (Fs / (2pi * 75000))
            // We scale by 0.5 further to leave 6dB headroom (so 75kHz deviation = 0.5 amplitude)
            _discriminatorGain = (fs2 / (float)(2 * Math.PI * 75000.0)) * 0.5f;

            // Channel Filter: 90kHz cutoff (180kHz BW)
            // Long enough to have a good transition band
            _channelFir = CreateLowPassFIR(63, 90000f, fs1); // 63 taps
            _firStateI = new float[63];
            _firStateQ = new float[63];

            // Audio Filter: 14.5kHz cutoff to reject 19kHz pilot
            // Fs=240k, cutoff=14.5k
            _audioFir = CreateLowPassFIR(63, 14500f, fs2); // 63 taps
            _audioFirState = new float[63];
        }

        public static float[] CreateLowPassFIR(int taps, float cutoffFreq, float sampleRate)
        {
            float[] h = new float[taps];
            float sum = 0;
            int center = taps / 2;
            float wc = 2 * (float)Math.PI * cutoffFreq / sampleRate;

            for (int i = 0; i < taps; i++)
            {
                if (i == center)
                    h[i] = wc / (float)Math.PI;
                else
                    h[i] = (float)Math.Sin(wc * (i - center)) / (float)(Math.PI * (i - center));

                // Hanning window
                h[i] *= (float)(0.5 * (1 - Math.Cos(2 * Math.PI * i / (taps - 1))));
                sum += h[i];
            }
            for (int i = 0; i < taps; i++) h[i] /= sum;
            return h;
        }

        public void Reset()
        {
            _lastI = 0;
            _lastQ = 0;
            _deemphasisState = 0;
            _mixerPhase = 0;
            Array.Clear(_firStateI, 0, _firStateI.Length);
            Array.Clear(_firStateQ, 0, _firStateQ.Length);
            Array.Clear(_audioFirState, 0, _audioFirState.Length);
        }

        public float[] Process(byte[] rawIq, int length)
        {
            int numSamples = length / 2;
            int dec1Samples = numSamples / Decimation1;
            float[] dec1I = new float[dec1Samples];
            float[] dec1Q = new float[dec1Samples];

            int taps1 = _channelFir.Length;
            int inIdx = 0;

            for (int i = 0; i < dec1Samples; i++)
            {
                for (int j = 0; j < Decimation1; j++)
                {
                    // Convert to centered float [-1.0, +1.0] approx
                    float I = (rawIq[inIdx++] - 127.5f) / 127.5f;
                    float Q = (rawIq[inIdx++] - 127.5f) / 127.5f;

                    // Digital mixer: shift by -fs/4 (-240kHz)
                    float mixI = 0, mixQ = 0;
                    switch (_mixerPhase)
                    {
                        case 0: mixI = I; mixQ = Q; break;
                        case 1: mixI = Q; mixQ = -I; break;
                        case 2: mixI = -I; mixQ = -Q; break;
                        case 3: mixI = -Q; mixQ = I; break;
                    }
                    _mixerPhase = (_mixerPhase + 1) & 3;

                    _firStateI[_firStateIdx] = mixI;
                    _firStateQ[_firStateIdx] = mixQ;

                    if (j == Decimation1 - 1)
                    {
                        float sumI = 0, sumQ = 0;
                        int idx = _firStateIdx;
                        for (int k = 0; k < taps1; k++)
                        {
                            sumI += _firStateI[idx] * _channelFir[k];
                            sumQ += _firStateQ[idx] * _channelFir[k];
                            if (--idx < 0) idx += taps1;
                        }
                        
                        // AM Limiter
                        float mag = (float)Math.Sqrt(sumI * sumI + sumQ * sumQ);
                        if (mag > 0.0001f) { sumI /= mag; sumQ /= mag; }
                        
                        dec1I[i] = sumI;
                        dec1Q[i] = sumQ;
                    }

                    if (++_firStateIdx >= taps1) _firStateIdx = 0;
                }
            }

            float[] demod = new float[dec1Samples];
            for (int i = 0; i < dec1Samples; i++)
            {
                float I = dec1I[i];
                float Q = dec1Q[i];

                float real = I * _lastI + Q * _lastQ;
                float imag = Q * _lastI - I * _lastQ;

                float phaseDiff = (float)Math.Atan2(imag, real);
                demod[i] = phaseDiff * _discriminatorGain; 

                _lastI = I;
                _lastQ = Q;
            }

            int outSamples = dec1Samples / Decimation2;
            float[] audio = new float[outSamples];

            int taps2 = _audioFir.Length;
            int dIdx = 0;

            for (int i = 0; i < outSamples; i++)
            {
                for (int j = 0; j < Decimation2; j++)
                {
                    _audioFirState[_audioFirStateIdx] = demod[dIdx++];
                    
                    if (j == Decimation2 - 1)
                    {
                        float sumA = 0;
                        int idx = _audioFirStateIdx;
                        for (int k = 0; k < taps2; k++)
                        {
                            sumA += _audioFirState[idx] * _audioFir[k];
                            if (--idx < 0) idx += taps2;
                        }
                        
                        // 50us De-emphasis at 48kHz
                        _deemphasisState = (1 - _deemphasisAlpha) * sumA + _deemphasisAlpha * _deemphasisState;
                        
                        float a = _deemphasisState;
                        if (a > 1.0f) a = 1.0f;
                        if (a < -1.0f) a = -1.0f;
                        audio[i] = a;
                    }
                    if (++_audioFirStateIdx >= taps2) _audioFirStateIdx = 0;
                }
            }

            return audio;
        }
    }
}
