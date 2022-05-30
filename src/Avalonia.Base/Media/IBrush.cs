using System.ComponentModel;
using Avalonia.Metadata;

namespace Avalonia.Media
{
    /// <summary>
    /// Describes how an area is painted.
    /// </summary>
    [TypeConverter(typeof(BrushConverter))]
    [NotClientImplementable]
    public interface IBrush
    {
        /// <summary>
        /// Gets the opacity of the brush.
        /// </summary>
        double Opacity { get; }

        /// <summary>
        /// Gets the transform of the brush.
        /// </summary>
        ITransform? Transform { get; }

        /// <summary>
        /// Gets the origin of the brushes <see cref="Transform"/>
        /// </summary>
        RelativePoint TransformOrigin { get; }
    }
}
