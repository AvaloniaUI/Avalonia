using System;
using System.IO;

using Avalonia.Direct2D1.Media.Imaging;
using Avalonia.Platform;

using D2DBitmap = SharpDX.Direct2D1.Bitmap1;

namespace Avalonia.Direct2D1.Media
{
    internal abstract class BitmapImpl : IBitmapImpl, IDisposable
    {
        public abstract Vector Dpi { get; }
        public abstract PixelSize PixelSize { get; }
        public int Version { get; protected set; } = 1;

        public abstract OptionalDispose<D2DBitmap> GetDirect2DBitmap(SharpDX.Direct2D1.RenderTarget target);

        public void Save(string fileName, int? quality = null)
        {
            var file = new FileInfo(fileName);
            var format = GetFormatFromExtension(file.Extension);
            using var s = new FileStream(fileName, FileMode.Create);
            Save(s, format, quality);
        }

        public abstract void Save(Stream stream, WicImageFormat format, int? quality = null);

        public void Save(Stream stream, int? quality = null)
        {
            Save(stream, WicImageFormat.Png, quality);
        }

        public virtual void Dispose()
        {
        }

        private static WicImageFormat GetFormatFromExtension(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".png" => WicImageFormat.Png,
                ".jpg" or ".jpeg" or ".jpe" => WicImageFormat.Jpeg,
                ".bmp" => WicImageFormat.Bmp,
                ".gif" => WicImageFormat.Gif,
                _ => throw new NotSupportedException("Unsupported image format.")
            };
        }
    }
}
