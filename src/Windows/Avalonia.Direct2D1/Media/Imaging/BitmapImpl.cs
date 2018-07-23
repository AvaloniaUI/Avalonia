using System;
using System.IO;
using Avalonia.Platform;
using SharpDX.WIC;
using D2DBitmap = SharpDX.Direct2D1.Bitmap;

namespace Avalonia.Direct2D1.Media
{
    public abstract class BitmapImpl : IBitmapImpl, IDisposable
    {
        public BitmapImpl(ImagingFactory imagingFactory)
        {
            WicImagingFactory = imagingFactory;
        }

        public ImagingFactory WicImagingFactory { get; }
        public abstract int PixelWidth { get; }
        public abstract int PixelHeight { get; }

        public abstract OptionalDispose<D2DBitmap> GetDirect2DBitmap(SharpDX.Direct2D1.RenderTarget target);

        public void Save(string fileName)
        {
            if (Path.GetExtension(fileName) != ".png")
            {
                // Yeah, we need to support other formats.
                throw new NotSupportedException("Use PNG, stoopid.");
            }

            using (FileStream s = new FileStream(fileName, FileMode.Create))
            {
                Save(s);
            }
        }

        public abstract void Save(Stream stream);

        public virtual void Dispose()
        {
        }
    }
}
