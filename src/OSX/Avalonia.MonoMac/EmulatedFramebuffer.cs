using System;
using Avalonia.Platform;
using MonoMac.AppKit;
using System.Runtime.InteropServices;
using MonoMac.CoreGraphics;

namespace Avalonia.MonoMac
{
    class EmulatedFramebuffer : ILockedFramebuffer
    {
        private readonly CGSize _logicalSize;
        public EmulatedFramebuffer(NSView view)
        {
            _logicalSize = view.Frame.Size;
            var pixelSize = view.ConvertSizeToBacking(_logicalSize);
            Width = (int)pixelSize.Width;
            Height = (int)pixelSize.Height;
            RowBytes = Width * 4;
            Dpi = new Vector(96 * pixelSize.Width / _logicalSize.Width, 96 * pixelSize.Height / _logicalSize.Height);
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
            using (var nscontext = NSGraphicsContext.CurrentContext)
            using (var context = nscontext.GraphicsPort)
            {
                context.SetFillColor(255, 255, 255, 255);
                context.FillRect(new CGRect(default(CGPoint), _logicalSize));
                context.DrawImage(new CGRect(default(CGPoint), _logicalSize), image);
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