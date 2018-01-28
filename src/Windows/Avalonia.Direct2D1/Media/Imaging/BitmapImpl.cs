using System;
using System.IO;
using Avalonia.Platform;
using SharpDX.WIC;
using D2DBitmap = SharpDX.Direct2D1.Bitmap;

namespace Avalonia.Direct2D1.Media
{
    public abstract class BitmapImpl : BitmapImplBase
    {
        public BitmapImpl(ImagingFactory imagingFactory)
        {
            WicImagingFactory = imagingFactory;
        }

        public ImagingFactory WicImagingFactory { get; }
        public override abstract int PixelWidth { get; }
        public override abstract int PixelHeight { get; }

        public abstract OptionalDispose<D2DBitmap> GetDirect2DBitmap(SharpDX.Direct2D1.RenderTarget target);

        public override void Save(string fileName)
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

        public override abstract void Save(Stream stream);

        protected override void DisposeCore()
        {
        }
    }
}
