using System.Collections.Generic;
using Avalonia.Media.Imaging;

namespace Avalonia.Wayland.Embedding.Compositor;

/// <summary>
/// One node in a flattened, draw-ordered surface tree handed compositor→UI. Carries only value data and the
/// opaque surface id; the actual pixels travel separately in <see cref="SurfaceCommit.NewBitmaps"/> so a
/// frame that only moves a subsurface doesn't recopy unchanged buffers.
/// </summary>
internal readonly struct SurfaceDrawItem
{
    public SurfaceDrawItem(uint surfaceId, double x, double y)
    {
        SurfaceId = surfaceId;
        X = x;
        Y = y;
    }

    public uint SurfaceId { get; }
    public double X { get; }
    public double Y { get; }
}

/// <summary>
/// An immutable snapshot of one root surface tree at commit time, produced on the compositor thread and
/// applied on the UI thread. <see cref="NewBitmaps"/> ownership transfers to the UI (freshly allocated).
/// </summary>
internal sealed class SurfaceCommit
{
    public SurfaceCommit(
        uint hostId,
        IReadOnlyList<SurfaceDrawItem> drawOrder,
        IReadOnlyDictionary<uint, Bitmap> newBitmaps,
        IReadOnlyList<uint> frameSurfaceIds,
        double clipWidth,
        double clipHeight)
    {
        HostId = hostId;
        DrawOrder = drawOrder;
        NewBitmaps = newBitmaps;
        FrameSurfaceIds = frameSurfaceIds;
        ClipWidth = clipWidth;
        ClipHeight = clipHeight;
    }

    public uint HostId { get; }
    public IReadOnlyList<SurfaceDrawItem> DrawOrder { get; }
    public IReadOnlyDictionary<uint, Bitmap> NewBitmaps { get; }

    /// <summary>Surfaces whose frame callbacks are deferred until the UI renders this commit.</summary>
    public IReadOnlyList<uint> FrameSurfaceIds { get; }

    /// <summary>Window-geometry clip in DIPs; 0 ⇒ unclipped (use natural content bounds).</summary>
    public double ClipWidth { get; }
    public double ClipHeight { get; }
}
