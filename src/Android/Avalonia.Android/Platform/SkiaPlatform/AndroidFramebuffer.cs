using System;
using System.Runtime.InteropServices;
using Android.Runtime;
using Android.Views;
using Avalonia.Platform;

namespace Avalonia.Android.Platform.SkiaPlatform
{
    class AndroidFramebuffer : ILockedFramebuffer
    {
        private IntPtr _window;

        public AndroidFramebuffer(Surface surface, double scaling)
        {
            if(surface == null)
                throw new ArgumentNullException(nameof(surface));
            _window = ANativeWindow_fromSurface(JNIEnv.Handle, surface.Handle);
            if (_window == IntPtr.Zero)
                throw new Exception("Unable to obtain ANativeWindow");
            ANativeWindow_Buffer buffer;
            var rc = new ARect()
            {
                right = ANativeWindow_getWidth(_window),
                bottom = ANativeWindow_getHeight(_window)
            };
            Size = new PixelSize(rc.right, rc.bottom);
            ANativeWindow_lock(_window, out buffer, ref rc);

            Format = buffer.format == AndroidPixelFormat.WINDOW_FORMAT_RGB_565
                ? PixelFormat.Rgb565 : PixelFormat.Rgba8888;

            RowBytes = buffer.stride * (Format == PixelFormat.Rgb565 ? 2 : 4);
            Address = buffer.bits;

            Dpi = new Vector(96, 96) * scaling;
        }

        public void Dispose()
        {
            ANativeWindow_unlockAndPost(_window);
            ANativeWindow_release(_window);
            _window = IntPtr.Zero;
            Address = IntPtr.Zero;
        }

        public IntPtr Address { get; set; }
        public PixelSize Size { get; }
        public int RowBytes { get; }
        public Vector Dpi { get; }
        public PixelFormat Format { get; }

        [DllImport("android")]
        internal static extern IntPtr ANativeWindow_fromSurface(IntPtr jniEnv, IntPtr handle);
        [DllImport("android")]
        internal static extern int ANativeWindow_getWidth(IntPtr window);
        [DllImport("android")]
        internal static extern int ANativeWindow_getHeight(IntPtr window);
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
#pragma warning disable CA1823 // Avoid unused private fields
            uint reserved1;
            uint reserved2;
            uint reserved3;
            uint reserved4;
            uint reserved5;
            uint reserved6;
#pragma warning restore CA1823 // Avoid unused private fields
        }
    }
}
