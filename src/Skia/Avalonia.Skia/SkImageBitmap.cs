using System.IO;
using Avalonia.Platform;
using Avalonia.Skia.Helpers;
using SkiaSharp;

namespace Avalonia.Skia
{
    class SkImageBitmap : IDrawableBitmapImpl, IBitmapImpl
    {
        private SKImage _image;

        public SkImageBitmap(SKImage image, Vector dpi)
        {
            Dpi = dpi;
            _image = image;
        }
        
        public void Dispose()
        {
            _image?.Dispose();
            _image = null;
        }

        public Vector Dpi { get; }
        public PixelSize PixelSize => new PixelSize(_image.Width, _image.Height);
        public int Version { get; } = 1;
        public void Save(string fileName) => ImageSavingHelper.SaveImage(_image, fileName);

        public void Save(Stream stream) => ImageSavingHelper.SaveImage(_image, stream);

        public void Draw(DrawingContextImpl context, SKRect sourceRect, SKRect destRect, SKPaint paint)
        {
            context.Canvas.DrawImage(_image, sourceRect, destRect, paint);
        }
    }
}
