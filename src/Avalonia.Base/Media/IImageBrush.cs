using Avalonia.Media.Imaging;
using Avalonia.Metadata;

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
        IBitmap Source { get; }
    }
}
