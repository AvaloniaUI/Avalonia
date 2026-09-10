using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server;

/// <summary>
/// Maps rects between a host's local space and the space its dirty-rect tracker stores in: texture pixels
/// for a cache host (mirroring the cache collector proxy's offset/scale), identity for the root host
/// (device pixels). Isolated here so the fiddly host-local ↔ tracker-px conversion has a single home and
/// dedicated tests.
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

    // A cache that has never drawn has a zero scale; nothing has been captured into its tracker yet, so
    // the mapping is simply unusable until the first draw sets a real scale.
    public bool IsUsable => ScaleX != 0 && ScaleY != 0;

    public bool IsIdentity => ScaleX == 1 && ScaleY == 1 && DrawAtOffset == default;

    /// <summary>Host-local rect → tracker (texture) space. Matches the cache collector proxy's AddRect.</summary>
    public LtrbRect HostToTracker(LtrbRect r) => new(
        (r.Left + DrawAtOffset.X) * ScaleX,
        (r.Top + DrawAtOffset.Y) * ScaleY,
        (r.Right + DrawAtOffset.X) * ScaleX,
        (r.Bottom + DrawAtOffset.Y) * ScaleY);

    /// <summary>Tracker (texture) space → host-local rect. Inverse of <see cref="HostToTracker"/>.</summary>
    public LtrbRect TrackerToHost(LtrbRect r) => new(
        r.Left / ScaleX - DrawAtOffset.X,
        r.Top / ScaleY - DrawAtOffset.Y,
        r.Right / ScaleX - DrawAtOffset.X,
        r.Bottom / ScaleY - DrawAtOffset.Y);
}
