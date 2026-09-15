using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.HitTesting;

internal struct GeometryCompositionHitTester : ICompositionHitTester<Geometry>
{
    public static Geometry Transform(Geometry input, in Matrix matrix)
    {
        var result = input.Clone();
        result.Transform = new MatrixTransform((input.Transform?.Value ?? Matrix.Identity) * matrix);
        return result;
    }

    public static IntersectionResult HitTest(CompositionVisual visual, Geometry input)
        => visual.HitTest(input);

    public static bool TransformedSubTreeBoundsMatch(LtrbRect bounds, Geometry input)
    {
        var geometryRenderBounds = input.Bounds;
        return bounds.Intersects(new LtrbRect(geometryRenderBounds));
    }

    public static bool ClippedBoundsMatch(CompositionVisual visual, Geometry input)
    {
        var bounds = input.Bounds;
        return bounds.Width > 0 && bounds.Height > 0 && bounds.Intersects(new Rect(new Size(visual.Size.X, visual.Size.Y)));
    }

    public static bool ClipMatches(IGeometryImpl clip, Geometry input)
        => input.PlatformImpl is { } geometryImpl &&
           clip.GetFillIntersectionResult(geometryImpl) > IntersectionResult.Empty;
}
