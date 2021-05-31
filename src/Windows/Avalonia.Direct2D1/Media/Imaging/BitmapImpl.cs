using System;
using System.IO;
using Avalonia.Platform;
using Vortice.Direct2D1;

namespace Avalonia.Direct2D1.Media
{
    public abstract class BitmapImpl : IBitmapImpl, IDisposable
    {
        public abstract Vector Dpi { get; }
        public abstract PixelSize PixelSize { get; }
        public int Version { get; protected set; } = 1;

        public abstract OptionalDispose<ID2D1Bitmap> GetDirect2DBitmap(ID2D1RenderTarget target);

        public void Save(string fileName)
        {
            if (Path.GetExtension(fileName) != ".png")
            {
                // Yeah, we need to support other formats.
                throw new NotSupportedException("Use PNG, stoopid.");
            }

            using (FileStream stream = new FileStream(fileName, FileMode.Create))
            {
                Save(stream);
            }
        }

        public abstract void Save(Stream stream);

        public virtual void Dispose()
        {
        }
    }
}
