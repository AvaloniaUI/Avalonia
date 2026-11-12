using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server;

/// <summary>
/// Maps rects between layer space and layer pixels. Identity for the root layer, the offset and scale of
/// <c>ServerCompositionVisualCache.DirtyRectCollectorProxy</c> for a bitmap cache layer.
/// </summary>
internal readonly struct DirtyRectSpaceMapping
{
    public static readonly DirtyRectSpaceMapping Identity = new(default, 1, 1);

    public readonly Vector DrawAtOffset;
    public readonly double ScaleX;
    public readonly double ScaleY;

    public DirtyRectSpaceMapping(Vector drawAtOffset, double scaleX, double scaleY)
    {
        DrawAtOffset = drawAtOffset;
        ScaleX = scaleX;
        ScaleY = scaleY;
    }

    // A bitmap cache has a zero scale until its first draw.
    public bool IsUsable => ScaleX != 0 && ScaleY != 0;

    public bool IsIdentity => ScaleX == 1 && ScaleY == 1 && DrawAtOffset == default;

    public LtrbRect LayerSpaceToLayerPixels(LtrbRect r) => new(
        (r.Left + DrawAtOffset.X) * ScaleX,
        (r.Top + DrawAtOffset.Y) * ScaleY,
        (r.Right + DrawAtOffset.X) * ScaleX,
        (r.Bottom + DrawAtOffset.Y) * ScaleY);

    public LtrbRect LayerPixelsToLayerSpace(LtrbRect r) => new(
        r.Left / ScaleX - DrawAtOffset.X,
        r.Top / ScaleY - DrawAtOffset.Y,
        r.Right / ScaleX - DrawAtOffset.X,
        r.Bottom / ScaleY - DrawAtOffset.Y);
}
