using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.HitTesting;

internal struct PointCompositionHitTester : ICompositionHitTester<Point>
{
    public static Point Transform(Point input, in Matrix matrix)
        => input.Transform(matrix);

    public static IntersectionResult HitTest(CompositionVisual visual, Point input)
        => visual.HitTest(input) ? IntersectionResult.FullyContains : IntersectionResult.Empty;

    public static bool TransformedSubTreeBoundsMatch(LtrbRect bounds, Point input)
        => bounds.Contains(input);

    public static bool ClippedBoundsMatch(CompositionVisual visual, Point input)
        => input.X >= 0 && input.Y >= 0 && input.X <= visual.Size.X && input.Y <= visual.Size.Y;

    public static bool ClipMatches(IGeometryImpl clip, Point input)
        => clip.FillContains(input);
}
