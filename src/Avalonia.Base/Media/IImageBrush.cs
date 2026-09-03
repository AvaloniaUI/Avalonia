using Avalonia.Media.Imaging;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Media
{
    /// <summary>
    /// Paints an area with an <see cref="IBitmap"/>.
    /// </summary>
    [NotClientImplementable]
    public interface IImageBrush : ITileBrush
    {
        /// <summary>
        /// Gets the image to draw.
        /// </summary>
        IImageBrushSource? Source { get; }
    }

    /// <summary>
    /// Provides access to the image source of an <see cref="IImageBrush"/>. 
    /// </summary>
    [NotClientImplementable]
    public interface IImageBrushSource
    {
        internal IRef<IBitmapImpl>? Bitmap { get; }

        /// <summary>
        /// Gets the bitmap implementation of the image source.
        /// </summary>
        /// <returns>
        /// The <see cref="IBitmapImpl"/> instance if available; otherwise, <c>null</c>.
        /// </returns>
        [PrivateApi]
        IBitmapImpl? GetBitmap() => Bitmap?.Item;
    }
}
