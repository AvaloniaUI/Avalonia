using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
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

        public EmulatedFramebuffer(UIView view)
        {
            var factor = (int) UIScreen.MainScreen.Scale;
            var frame = view.Frame;
            Width = (int) frame.Width * factor;
            Height = (int) frame.Height * factor;
            RowBytes = Width * 4;
            Dpi = new Vector(96, 96) * factor;
            Format = PixelFormat.Rgba8888;
            Address = Marshal.AllocHGlobal(Height * RowBytes);
        }

        public void Dispose()
        {
            if (Address == IntPtr.Zero)
                return;
            var nfo = (int) CGBitmapFlags.ByteOrder32Big | (int) CGImageAlphaInfo.PremultipliedLast;
            using (var colorSpace = CGColorSpace.CreateDeviceRGB())
            using (var bContext = new CGBitmapContext(Address, Width, Height, 8, Width * 4,
                colorSpace, (CGImageAlphaInfo) nfo))
            using (var image = bContext.ToImage())
            using (var context = UIGraphics.GetCurrentContext())
            {
                // flip the image for CGContext.DrawImage
                context.TranslateCTM(0, Height);
                context.ScaleCTM(1, -1);
                context.DrawImage(new CGRect(0, 0, Width, Height), image);
            }
            Marshal.FreeHGlobal(Address);
            Address = IntPtr.Zero;
        }

        public IntPtr Address { get; private set; }
        public int Width { get; }
        public int Height { get; }
        public int RowBytes { get; }
        public Vector Dpi { get; }
        public PixelFormat Format { get; }
    }
}
