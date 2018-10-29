using System;
using System.Runtime.InteropServices;
using Avalonia.Platform;
using CoreGraphics;
using UIKit;

namespace Avalonia.iOS
{

    /// <summary>
    /// This is a bit weird, but CG doesn't provide proper bitmap
    /// with lockable bits, but can create one from data pointer
    /// So we are using our own buffer here.
    /// </summary>
    class EmulatedFramebuffer : ILockedFramebuffer
    {
        private nfloat _viewWidth;
        private nfloat _viewHeight;

        public EmulatedFramebuffer(UIView view)
        {
            var factor = (int) UIScreen.MainScreen.Scale;
            var frame = view.Frame;
            _viewWidth = frame.Width;
            _viewHeight = frame.Height;
            Size = new PixelSize((int)frame.Width * factor, (int)frame.Height * factor);
            RowBytes = Size.Width * 4;
            Dpi = new Vector(96, 96) * factor;
            Format = PixelFormat.Rgba8888;
            Address = Marshal.AllocHGlobal(Size.Height * RowBytes);
        }

        public void Dispose()
        {
            if (Address == IntPtr.Zero)
                return;
            var nfo = (int) CGBitmapFlags.ByteOrder32Big | (int) CGImageAlphaInfo.PremultipliedLast;
            using (var colorSpace = CGColorSpace.CreateDeviceRGB())
            using (var bContext = new CGBitmapContext(Address, Size.Width, Size.Height, 8, Size.Width * 4,
                colorSpace, (CGImageAlphaInfo) nfo))
            using (var image = bContext.ToImage())
            using (var context = UIGraphics.GetCurrentContext())
            {
                // flip the image for CGContext.DrawImage
                context.TranslateCTM(0, _viewHeight);
                context.ScaleCTM(1, -1);
                context.DrawImage(new CGRect(0, 0, _viewWidth, _viewHeight), image);
            }
            Marshal.FreeHGlobal(Address);
            Address = IntPtr.Zero;
        }

        public IntPtr Address { get; private set; }
        public PixelSize Size { get; }
        public int RowBytes { get; }
        public Vector Dpi { get; }
        public PixelFormat Format { get; }
    }
}

