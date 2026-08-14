# DubboSDR

DubboSDR is a C# .NET 8 WPF application for exploring local FM radio in Dubbo, NSW, Australia, using an RTL-SDR dongle (specifically tested with the **Nooelec NESDR SMArt v5**).

## Hardware & OS Requirements
- **Hardware:** Nooelec NESDR SMArt v5 (or compatible RTL-SDR).
- **Driver:** WinUSB installed via Zadig (SDR++ compatibility).
- **OS:** Windows x64.
- **Framework:** .NET 8 SDK.

## Features
- **Kids Radio:** A touch-friendly, ultra-simple UI for young children to tune between predefined stations.
- **Explorer:** A more technical interface displaying frequency, raw signal strength, hardware offsets, and clipping stats.
- **Live Phone Streaming (WIP):** A backend ASP.NET Core Kestrel server to stream the live decoded audio to a paired mobile browser.

## Native Dependencies
This project relies directly on `rtlsdr.dll` and `pthreadVC2.dll`. We bundle the exact known-good binary distributions in `DubboSDR.RtlSdr/native/` to ensure immediate compatibility out of the box. See `THIRD_PARTY_NOTICES.md` for licensing details.

## Build and Run
1. Ensure .NET 8 SDK is installed.
2. Ensure your RTL-SDR is plugged in and WinUSB driver is active.
3. Run the application:
```powershell
cd src/DubboSDR.App
dotnet run
```

## Known Limitations
- Internet Audio streams are not yet implemented.
- Station retuning can occasionally lock the backend if the RTL-SDR USB packet drops during the semaphore lock, though the `TuneAsync` 14-step architecture mitigates this.
