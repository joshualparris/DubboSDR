# Architecture

## SDR Backend (`RadioDeviceManager`)
DubboSDR talks directly to the RTL-SDR via standard C-style P/Invoke calls (`rtlsdr.dll`). 
The core architecture consists of:
- **`ReadLoop`:** A background task continuously pulling 128 KB blocks via `rtlsdr_read_sync`.
- **`FmDemodulator`:** Performs DSP processing: Frequency shift → Decimation → Complex channel LPF → Discriminator → Audio scaling/De-emphasis.
- **`AudioPlayer`:** Feeds a bounded `BufferedWaveProvider` that NAudio continuously drains to the default Windows audio device.

## Thread Safety & The "14-Step Retune"
Because `rtlsdr_read_sync` blocks deeply inside the USB library, changing frequency concurrently is fatal. DubboSDR employs a strict asynchronous locking mechanism in `TuneAsync()`:
1. Acquire `SemaphoreSlim`.
2. Cancel the active `CancellationToken` for the read loop.
3. Block until `ReadLoop` completes its current frame and gracefully terminates.
4. Issue `rtlsdr_set_center_freq` and `rtlsdr_reset_buffer`.
5. Start a new `ReadLoop`.
6. Only return success once the audio buffer verifies fresh samples have propagated.

## Audio Tap (For Remote Streaming)
(Under Construction)
Instead of feeding NAudio exclusively, the processed 48 kHz mono PCM output from `FmDemodulator.Process` will be multicast:
1. `NAudio.BufferedWaveProvider` (Local Desktop Playback)
2. `ConcurrentQueue<byte[]>` or Bounded Ring Buffer (Remote Streaming API)
