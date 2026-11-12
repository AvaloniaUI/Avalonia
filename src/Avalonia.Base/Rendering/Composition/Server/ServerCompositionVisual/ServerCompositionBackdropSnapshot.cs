using System;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server;

/// <summary>
/// The backdrop snapshot of one retained backdrop visual: a copy of its input with the size of the backdrop
/// AABB in layer pixels. Unlike a bitmap cache it never draws visuals, it only copies pixels from the layer.
/// Owned by <see cref="ServerCompositionVisualBackdropState.Snapshot"/>.
/// </summary>
internal sealed class ServerCompositionBackdropSnapshot : IDisposable
{
    private IDrawingContextBackdropSnapshotImpl? _impl;
    private PixelSize _size;
    private IPlatformRenderInterfaceContext? _context;
    // Reused, so that a draw does not allocate.
    private readonly PixelRect[] _singleDirtyRect = new PixelRect[1];

    public void Dispose()
    {
        _impl?.Dispose();
        _impl = null;
        _size = default;
        _context = null;
    }

    /// <summary>
    /// Refreshes the snapshot from <paramref name="inputDirtyRect"/> and draws it through
    /// <paramref name="effect"/>.
    /// </summary>
    /// <param name="canvas">The drawing context of the layer.</param>
    /// <param name="renderContext">The render context that the snapshot is created for.</param>
    /// <param name="aabb">The backdrop AABB in layer pixels.</param>
    /// <param name="inputDirtyRect">In layer pixels and inside <paramref name="aabb"/>, or null.</param>
    /// <param name="effect">The backdrop effect.</param>
    /// <param name="bounds">The backdrop bounds in the current coordinate space of the canvas.</param>
    public void Draw(IDrawingContextImplWithBackdropSupport canvas, IPlatformRenderInterfaceContext renderContext,
        LtrbPixelRect aabb, LtrbPixelRect? inputDirtyRect, IEffect effect, Rect bounds)
    {
        var size = new PixelSize(aabb.Right - aabb.Left, aabb.Bottom - aabb.Top);
        if (size.Width < 1 || size.Height < 1)
            return;

        // The render context is recreated after a device loss, and a snapshot of the old one is unusable.
        // Same check as in ServerCompositionVisualCache.
        var created = false;
        if (_impl == null || _size != size || _context != renderContext)
        {
            _impl?.Dispose();
            _impl = canvas.CreateBackdropSnapshot(size);
            _size = size;
            _context = renderContext;
            created = true;
        }

        PixelRect[] dirtyRects;
        if (created)
        {
            // A new snapshot is empty: refresh the whole AABB.
            _singleDirtyRect[0] = aabb.ToPixelRect();
            dirtyRects = _singleDirtyRect;
        }
        else if (inputDirtyRect is { } dirty && dirty.Right > dirty.Left && dirty.Bottom > dirty.Top)
        {
            _singleDirtyRect[0] = dirty.ToPixelRect();
            dirtyRects = _singleDirtyRect;
        }
        else
            dirtyRects = Array.Empty<PixelRect>();

        canvas.DrawRetainedBackdropEffect(_impl!, aabb.ToPixelRect(), dirtyRects, effect, bounds);
    }
}
