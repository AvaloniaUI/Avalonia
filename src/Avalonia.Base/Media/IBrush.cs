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

        /// <summary>
        /// Gets the transform applied in the relative coordinate space of the area being painted:
        /// the unit square (0,0)-(1,1) maps onto the painted bounds, this transform applies inside
        /// that space, and <see cref="Transform"/> follows in target space. It lets a single brush
        /// express a bounds-dependent transform - an SVG gradientTransform in objectBoundingBox
        /// units, for example - without baking any one consumer's bounds into a matrix.
        /// </summary>
        ITransform? RelativeTransform { get; }
    }
}
