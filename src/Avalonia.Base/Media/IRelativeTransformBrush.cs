namespace Avalonia.Media;

/// <summary>
/// A brush that can be transformed in the relative coordinate space of the area
/// being painted: the unit square (0,0)-(1,1) maps onto the target bounds,
/// <see cref="RelativeTransform"/> applies inside that space, and
/// <see cref="IBrush.Transform"/> follows in target space. This lets a single
/// brush express a bounds-dependent transform - for example an SVG
/// gradientTransform in objectBoundingBox units - without baking any one
/// consumer's bounds into a matrix.
/// </summary>
// TODO13: fold RelativeTransform into IGradientBrush.
public interface IRelativeTransformBrush : IBrush
{
    /// <summary>
    /// Gets the transform applied in the unit space of the painted bounds,
    /// before <see cref="IBrush.Transform"/>.
    /// </summary>
    ITransform? RelativeTransform { get; }
}
