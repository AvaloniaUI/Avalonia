using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Avalonia.Skia.Android
{
    static class NativeMethods
    {
        [DllImport("android")]
        internal static extern IntPtr ANativeWindow_fromSurface(IntPtr jniEnv, IntPtr handle);

        [DllImport("android")]
        internal static extern void ANativeWindow_release(IntPtr window);
        [DllImport("android")]
        internal static extern void ANativeWindow_unlockAndPost(IntPtr window);

        [DllImport("android")]
        internal static extern int ANativeWindow_lock(IntPtr window, out ANativeWindow_Buffer outBuffer, ref ARect inOutDirtyBounds);
        public enum AndroidPixelFormat
        {
            WINDOW_FORMAT_RGBA_8888 = 1,
            WINDOW_FORMAT_RGBX_8888 = 2,
            WINDOW_FORMAT_RGB_565 = 4,
        }

        internal struct ARect
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }


        internal struct ANativeWindow_Buffer
        {
            // The number of pixels that are show horizontally.
            public int width;

            // The number of pixels that are shown vertically.
            public int height;

            // The number of *pixels* that a line in the buffer takes in
            // memory.  This may be >= width.
            public int stride;

            // The format of the buffer.  One of WINDOW_FORMAT_*
            public AndroidPixelFormat format;

            // The actual bits.
            public IntPtr bits;

            // Do not touch.
            uint reserved1;
            uint reserved2;
            uint reserved3;
            uint reserved4;
            uint reserved5;
            uint reserved6;
        }
    }
}