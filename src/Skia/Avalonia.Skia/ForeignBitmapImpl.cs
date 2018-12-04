using System;
using System.IO;
using System.Reactive.Disposables;
using System.Threading;
using Avalonia.Platform;
using Avalonia.Skia.Helpers;
using SkiaSharp;

namespace Avalonia.Skia
{
    class ForeignBitmapImpl : IForeignBitmapImpl, IDrawableBitmapImpl
    {
        private SKImage _image;
        private readonly bool _ownsImage;
        private readonly object _lock = new object();
        private int _version;

        public ForeignBitmapImpl(SKImage image, Vector dpi, bool ownsImage)
        {
            _image = image;
            _ownsImage = ownsImage;
            Dpi = dpi;
            PixelSize = new PixelSize(image.Width, image.Height);

        }
        public void Dispose()
        {
            lock (_lock)
            {
                if (_ownsImage)
                {
                    _image?.Dispose();
                }

                _image = null;
            }
        }

        public Vector Dpi { get; }
        public PixelSize PixelSize { get; }

        public int Version
        {
            get
            {
                lock (_lock)
                    return _version;
            }
        }

        public void Save(string fileName)
        {
            lock (_image)
                ImageSavingHelper.SaveImage(_image, fileName);
        }

        public void Save(Stream stream)
        {
            lock (_image)
                ImageSavingHelper.SaveImage(_image, stream);
        }

        public void Draw(DrawingContextImpl context, SKRect sourceRect, SKRect destRect, SKPaint paint)
        {
            lock (_lock)
                context.Canvas.DrawImage(_image, sourceRect, destRect, paint);
        }

        public IDisposable Lock()
        {
            Monitor.Enter(_lock);
            return Disposable.Create(() =>
            {
                _version++;
                Monitor.Exit(_lock);
            });
        }
    }
}
