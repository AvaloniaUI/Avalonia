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

    [NotClientImplementable]
    public interface IImageBrushSource
    {
        internal IRef<IBitmapImpl>? Bitmap { get; }
    }
}
