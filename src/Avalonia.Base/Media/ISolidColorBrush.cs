using Avalonia.Metadata;

namespace Avalonia.Media
{
    /// <summary>
    /// Fills an area with a solid color.
    /// </summary>
    [NotClientImplementable]
    public interface ISolidColorBrush : IBrush
    {
        /// <summary>
        /// Gets the color of the brush.
        /// </summary>
        Color Color { get; }
    }

    /// <summary>
    /// Fills an area with a solid color.
    /// </summary>
    [NotClientImplementable]
    public interface IImmutableSolidColorBrush : ISolidColorBrush, IImmutableBrush
    {
        
    }
}
