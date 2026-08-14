using System;

namespace DubboSDR.Core
{
    public class NfmDemodulator
    {
        private float _lastI = 0;
        private float _lastQ = 0;
        private int _mixerPhase = 0;

        public const int Decimation1 = 8; // 960k -> 120k
        public const int Decimation2 = 10; // 120k -> 12k

        public int OutputSampleRate { get; } = 12000;

        private float[] _channelFir;
        private float[] _audioFir;

        private float[] _firStateI;
        private float[] _firStateQ;
        private int _firStateIdx = 0;
        
        private float[] _audioFirState;
        private int _audioFirStateIdx = 0;

        private readonly float _discriminatorGain;

        public NfmDemodulator()
        {
            float fs1 = 960000f;
            float fs2 = fs1 / Decimation1; // 120,000 Hz
            float fsAudio = fs2 / Decimation2; // 12,000 Hz

            // NBFM Discriminator scale: 2.5kHz deviation = 1.0 amplitude
            _discriminatorGain = (fs2 / (float)(2 * Math.PI * 2500.0)) * 0.5f;

            // Channel Filter: 6.25kHz cutoff (12.5kHz BW for UHF CB)
            _channelFir = FmDemodulator.CreateLowPassFIR(63, 6250f, fs1);
            _firStateI = new float[63];
            _firStateQ = new float[63];

            // Audio Filter: 3kHz cutoff (Voice)
            _audioFir = FmDemodulator.CreateLowPassFIR(63, 3000f, fs2);
            _audioFirState = new float[63];
        }

        public void Reset()
        {
            _lastI = 0;
            _lastQ = 0;
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
                    float I = (rawIq[inIdx++] - 127.5f) / 127.5f;
                    float Q = (rawIq[inIdx++] - 127.5f) / 127.5f;

                    // Standard center mixing if needed, let's keep phase 0 for now 
                    // assuming we tuned exactly to the NBFM channel.
                    float mixI = I, mixQ = Q;
                    
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
                        
                        float a = sumA;
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
