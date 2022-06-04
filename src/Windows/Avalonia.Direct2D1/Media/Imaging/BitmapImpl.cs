﻿using System;
using System.IO;
using Avalonia.Metadata;
using Avalonia.Platform;
using D2DBitmap = SharpDX.Direct2D1.Bitmap;

namespace Avalonia.Direct2D1.Media
{
    [Unstable]
    public abstract class BitmapImpl : IBitmapImpl, IDisposable
    {
        public abstract Vector Dpi { get; }
        public abstract PixelSize PixelSize { get; }
        public int Version { get; protected set; } = 1;

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
