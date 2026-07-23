using Avalonia.Media;
using Avalonia.Media.Immutable;
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

    public static bool HitTest(CompositionVisual visual, Geometry input)
        => visual.HitTest(input) > IntersectionDetail.Empty;

    public static bool TransformedSubTreeBoundsMatch(LtrbRect bounds, Geometry input)
    {
        var pen = new ImmutablePen(Colors.Black.ToUInt32(), 0);
        var geometryRenderBounds = input.GetRenderBounds(pen);
        return bounds.Overlaps(new LtrbRect(geometryRenderBounds));
    }

    public static bool AabbNodeBoundsMatch(LtrbRect bounds, Geometry input)
    {
        // TODO: verify, why are we using a different logic in the AABB tree case?
        return bounds.Overlaps(new LtrbRect(input.Bounds));
    }

    public static bool ClippedBoundsMatch(CompositionVisual visual, Geometry input)
    {
        var bounds = input.Bounds;
        return bounds.Width > 0 && bounds.Height > 0;
    }

    public static bool ClipMatches(IGeometryImpl clip, Geometry input)
        => input.PlatformImpl is { } geometryImpl &&
           clip.FillContains(geometryImpl) > IntersectionDetail.Empty;
}
