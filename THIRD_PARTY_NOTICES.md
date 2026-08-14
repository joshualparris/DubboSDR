# Third-Party Notices

DubboSDR uses the following third-party software and libraries:

## RTL-SDR Native Libraries (`rtlsdr.dll`, `libusb-1.0.dll`, `pthreadVC2.dll`)
DubboSDR interfaces directly with native RTL-SDR drivers.
These binary components were originally sourced from the [SDR++](https://github.com/AlexandreRouma/SDRPlusPlus) Windows x64 distribution and [Osmocom rtl-sdr](https://osmocom.org/projects/rtl-sdr/wiki/Rtl-sdr).
- **libusb-1.0**: Licensed under LGPL.
- **pthreadVC2**: POSIX Threads for Windows, licensed under LGPL.
- **rtlsdr**: Licensed under GPLv2+.

## NAudio
Audio playback and buffering is powered by [NAudio](https://github.com/naudio/NAudio).
- **License**: MIT License

## .NET 8 / WPF
The application is built on Microsoft's .NET 8 Framework and Windows Presentation Foundation.
- **License**: MIT License
