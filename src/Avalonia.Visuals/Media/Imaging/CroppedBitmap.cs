using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Visuals.Media.Imaging;

namespace Avalonia.Media.Imaging
{
    public class CroppedBitmap : IImage, IDisposable
    {
        public CroppedBitmap()
        {
            Source = null;
            SourceRect = default;
        }
        public CroppedBitmap(IBitmap source, PixelRect sourceRect)
        {
            Source = source;
            SourceRect = sourceRect;
        }
        public virtual void Dispose()
        {
            Source?.Dispose();
        }

        public Size Size {
            get
            {
                if (Source == null)
                    return Size.Empty;
                if (SourceRect.IsEmpty)
                    return Source.Size;
                return SourceRect.Size.ToSizeWithDpi(Source.Dpi);
            }
        }

        public void Draw(DrawingContext context, Rect sourceRect, Rect destRect, BitmapInterpolationMode bitmapInterpolationMode)
        {
            if (Source == null)
                return;
            var topLeft = SourceRect.TopLeft.ToPointWithDpi(Source.Dpi);
            Source.Draw(context, sourceRect.Translate(new Vector(topLeft.X, topLeft.Y)), destRect, bitmapInterpolationMode);           
        }

        public IBitmap Source { get; }
        public PixelRect SourceRect { get; }
    }
}
