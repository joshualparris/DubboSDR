using System;
using System.Runtime.InteropServices;

namespace DubboSDR.RtlSdr.Native
{
    public unsafe delegate void RtlSdrReadAsyncCb(byte* buf, uint len, IntPtr ctx);

    public static class RtlSdrNative
    {
        private const string DllName = "native\\rtlsdr.dll";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint rtlsdr_get_device_count();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr rtlsdr_get_device_name(uint index);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_open(out IntPtr dev, uint index);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_close(IntPtr dev);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_set_center_freq(IntPtr dev, uint freq);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint rtlsdr_get_center_freq(IntPtr dev);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_set_sample_rate(IntPtr dev, uint rate);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_set_tuner_gain_mode(IntPtr dev, int manual);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_set_tuner_gain(IntPtr dev, int gain);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_get_tuner_gain(IntPtr dev);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_get_tuner_gains(IntPtr dev, int[] gains);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_reset_buffer(IntPtr dev);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_read_sync(IntPtr dev, IntPtr buf, int len, out int n_read);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_read_async(IntPtr dev, RtlSdrReadAsyncCb cb, IntPtr ctx, uint buf_num, uint buf_len);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_cancel_async(IntPtr dev);
    }
}
