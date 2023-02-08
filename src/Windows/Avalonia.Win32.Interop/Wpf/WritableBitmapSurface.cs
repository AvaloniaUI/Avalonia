using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Platform;
using PixelFormat = Avalonia.Platform.PixelFormat;

namespace Avalonia.Win32.Interop.Wpf
{
    class WritableBitmapSurface : IFramebufferPlatformSurface
    {
        private readonly WpfTopLevelImpl _impl;
        private WriteableBitmap _bitmap;
        public WritableBitmapSurface(WpfTopLevelImpl impl)
        {
            _impl = impl;
        }

        public ILockedFramebuffer Lock()
        {
            var scale = _impl.GetScaling();
            var size = new Size(_impl.ActualWidth * scale.X, _impl.ActualHeight * scale.Y);
            var dpi = scale * 96;
            if (_bitmap == null || _bitmap.PixelWidth != (int) size.Width || _bitmap.PixelHeight != (int) size.Height)
            {
                _bitmap = new WriteableBitmap((int) size.Width, (int) size.Height, dpi.X, dpi.Y,
                    System.Windows.Media.PixelFormats.Bgra32, null);
            }
            return new LockedFramebuffer(_impl, _bitmap, dpi);
        }

        internal class LockedFramebuffer : ILockedFramebuffer
        {
            private readonly WpfTopLevelImpl _impl;
            private readonly WriteableBitmap _bitmap;

            public LockedFramebuffer(WpfTopLevelImpl impl, WriteableBitmap bitmap, Vector dpi)
            {
                _impl = impl;
                _bitmap = bitmap;
                Dpi = dpi;
                _bitmap.Lock();
            }

            public void Dispose()
            {
                _bitmap.AddDirtyRect(new Int32Rect(0, 0, _bitmap.PixelWidth, _bitmap.PixelHeight));
                _bitmap.Unlock();
                /*
                using (var fileStream = new FileStream("c:\\tools\\wat.png", FileMode.Create))
                {
                    BitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(_bitmap));
                    encoder.Save(fileStream);
                }*/
                _impl.ImageSource = _bitmap;
            }

            public IntPtr Address => _bitmap.BackBuffer;
            public PixelSize Size => new PixelSize(_bitmap.PixelWidth, _bitmap.PixelHeight);
            public int RowBytes => _bitmap.BackBufferStride;
            public Vector Dpi { get; }
            public PixelFormat Format => PixelFormat.Bgra8888;
        }
    }
}
