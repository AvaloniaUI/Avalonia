using System;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server;

/// <summary>
/// The backdrop snapshot of one retained backdrop visual: a copy of its input. It covers the part of the backdrop
/// AABB that is inside of the layer. Unlike a bitmap cache it never draws visuals, it only copies pixels from the
/// layer.
/// Owned by <see cref="ServerCompositionVisualBackdropState.Snapshot"/>.
/// </summary>
internal sealed class ServerCompositionBackdropSnapshot : IDisposable
{
    private IDrawingContextBackdropSnapshotImpl? _impl;
    private PixelSize _size;
    private PixelRect _bounds;
    private IPlatformRenderInterfaceContext? _context;
    // Reused, so that a draw does not allocate.
    private readonly PixelRect[] _singleDirtyRect = new PixelRect[1];

    /// <summary>The area of the layer that the snapshot covers, in layer pixels.</summary>
    public PixelRect Bounds => _bounds;

    public void Dispose()
    {
        _impl?.Dispose();
        _impl = null;
        _size = default;
        _bounds = default;
        _context = null;
    }

    /// <summary>
    /// Refreshes the snapshot from <paramref name="inputDirtyRect"/> and draws it through
    /// <paramref name="effect"/>.
    /// </summary>
    /// <param name="canvas">The drawing context of the layer.</param>
    /// <param name="renderContext">The render context that the snapshot is created for.</param>
    /// <param name="snapshotBounds">The part of the backdrop AABB inside of the layer, in layer pixels.</param>
    /// <param name="inputDirtyRect">In layer pixels and inside <paramref name="snapshotBounds"/>, or null.</param>
    /// <param name="effect">The backdrop effect.</param>
    /// <param name="backdropBounds">The backdrop bounds in the current coordinate space of the canvas.</param>
    public void Draw(IDrawingContextImplWithBackdropSupport canvas, IPlatformRenderInterfaceContext renderContext,
        LtrbPixelRect snapshotBounds, LtrbPixelRect? inputDirtyRect, IEffect effect, Rect backdropBounds)
    {
        var bounds = snapshotBounds.ToPixelRect();
        if (bounds.Width < 1 || bounds.Height < 1)
            return;

        // The render context is recreated after a device loss, and a snapshot of the old one is unusable.
        // Same check as in ServerCompositionVisualCache.
        var created = false;
        if (_impl == null || _size != bounds.Size || _context != renderContext)
        {
            _impl?.Dispose();
            _impl = canvas.CreateBackdropSnapshot(bounds.Size);
            _size = bounds.Size;
            _context = renderContext;
            created = true;
        }

        PixelRect[] dirtyRects;
        if (created || _bounds != bounds)
        {
            // A new snapshot is empty, and a moved one has pixels of another place: refresh all of it.
            _singleDirtyRect[0] = bounds;
            dirtyRects = _singleDirtyRect;
        }
        else if (inputDirtyRect is { } dirty && dirty.Right > dirty.Left && dirty.Bottom > dirty.Top)
        {
            _singleDirtyRect[0] = dirty.ToPixelRect();
            dirtyRects = _singleDirtyRect;
        }
        else
            dirtyRects = Array.Empty<PixelRect>();

        _bounds = bounds;
        canvas.DrawRetainedBackdropEffect(_impl!, bounds, dirtyRects, effect, backdropBounds);
    }
}
