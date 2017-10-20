using System;
using Avalonia.Platform;
using MonoMac.AppKit;
using System.Runtime.InteropServices;
using Avalonia.Threading;
using MonoMac.CoreGraphics;

namespace Avalonia.MonoMac
{
    class EmulatedFramebuffer : ILockedFramebuffer
    {
        private readonly TopLevelImpl.TopLevelView _view;
        private readonly CGSize _logicalSize;

        public EmulatedFramebuffer(TopLevelImpl.TopLevelView view, CGSize logicalSize, CGSize pixelSize)
        {
            _view = view;
            _logicalSize = logicalSize;
            Width = (int)pixelSize.Width;
            Height = (int)pixelSize.Height;
            RowBytes = Width * 4;
            Dpi = new Vector(96 * pixelSize.Width / _logicalSize.Width, 96 * pixelSize.Height / _logicalSize.Height);
            Format = PixelFormat.Rgba8888;
            Address = Marshal.AllocHGlobal(Height * RowBytes);
        }

        class DeferredRenderingHelper : IDisposable
        {
            private readonly NSView _view;
            public void Dispose()
            {
                _view.NonUIUnlockFocus();
            }

            public DeferredRenderingHelper(NSView view)
            {
                _view = view;
            }
        }

        DeferredRenderingHelper SetupWindowContext()
        {
            if (!_view.NonUILockFocusIfCanDraw())
                return null;
            return new DeferredRenderingHelper(_view);
        }

        public void Dispose()
        {
            if (Address == IntPtr.Zero)
                return;
            var nfo = (int) CGBitmapFlags.ByteOrder32Big | (int) CGImageAlphaInfo.PremultipliedLast;

            DeferredRenderingHelper deferred = null;
            var isDeferred = !Dispatcher.UIThread.CheckAccess();
            if (!isDeferred || (deferred = SetupWindowContext()) != null)
            {
                CGImage image = null;
                try
                {
                    lock (_view.SyncRoot)
                    {
                        using (deferred)
                        using (var colorSpace = CGColorSpace.CreateDeviceRGB())
                        using (var bContext = new CGBitmapContext(Address, Width, Height, 8, Width * 4,
                            colorSpace, (CGImageAlphaInfo) nfo))
                        {
                            if (!isDeferred || deferred != null)
                            {
                                using (var nscontext = NSGraphicsContext.CurrentContext)
                                using (var context = nscontext.GraphicsPort)
                                {
                                    image = bContext.ToImage();
                                    context.SetFillColor(255, 255, 255, 255);
                                    context.FillRect(new CGRect(default(CGPoint), _view.LogicalSize));
                                    context.TranslateCTM(0, _view.LogicalSize.Height - _logicalSize.Height);
                                    context.DrawImage(new CGRect(default(CGPoint), _logicalSize), image);
                                    context.Flush();
                                    nscontext.FlushGraphics();
                                }
                            }
                        }
                    }
                }
                finally
                {
                    if (image != null)
                    {
                        if (deferred == null)
                            image.Dispose();
                        else
                            _view.SetBackBufferImage(new SavedImage(image, _logicalSize));
                    }
                }
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

    class SavedImage : IDisposable
    {
        public CGImage Image { get; private set; }
        public CGSize LogicalSize { get; }

        public SavedImage(CGImage image, CGSize logicalSize)
        {
            Image = image;
            LogicalSize = logicalSize;
        }

        public void Dispose()
        {
            Image?.Dispose();
            Image = null;
        }
    }
}