using System;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server;

/// <summary>
/// Retained capture texture for a single backdrop visual. Holds an offscreen cache, sized to the
/// backdrop's surface-space pixel AABB, into which freshly damaged backdrop pixels are re-ingested;
/// the cache is then drawn back through the effect. Analogous to <see cref="ServerCompositionVisualCache"/>
/// but far simpler: it never runs the visual tree, it only blits snapshotted target pixels and draws.
/// Owned by the visual's <see cref="ServerCompositionVisualBackdropState.RetainedState"/>; disposed with it.
/// </summary>
internal sealed class ServerCompositionBackdropStore : IDisposable
{
    private IDrawingContextBackdropCacheImpl? _cache;
    private PixelSize _cacheSize;
    private IPlatformRenderInterfaceContext? _cacheContext;
    // Reused single-element damage list, so the common per-frame draw doesn't allocate a fresh array.
    private readonly PixelRect[] _singleDirtyRect = new PixelRect[1];

    public void Dispose()
    {
        _cache?.Dispose();
        _cache = null;
        _cacheSize = default;
        _cacheContext = null;
    }

    /// <summary>
    /// Re-ingests any pending input and draws the retained backdrop through <paramref name="effect"/>.
    /// </summary>
    /// <param name="canvas">The live, surface-backed target context the backdrop is being drawn into.</param>
    /// <param name="renderContext">The persistent render context the cache is bound to (used for affinity).</param>
    /// <param name="surfaceAabb">The backdrop AABB in surface pixels; the layer covers exactly this rect.</param>
    /// <param name="pending">Pending input to re-ingest, in surface pixels (already ∩ AABB), or null.</param>
    /// <param name="effect">The backdrop effect.</param>
    /// <param name="destRect">The backdrop AABB in the canvas' current coordinate space.</param>
    public void Draw(IDrawingContextImplWithBackdropSupport canvas, IPlatformRenderInterfaceContext renderContext,
        LtrbPixelRect surfaceAabb, LtrbPixelRect? pending, IEffect effect, Rect destRect)
    {
        var size = new PixelSize(surfaceAabb.Right - surfaceAabb.Left, surfaceAabb.Bottom - surfaceAabb.Top);
        if (size.Width < 1 || size.Height < 1)
            return;

        // Reallocate on a pixel-size change, or when the creating render context is gone: a GPU device-loss
        // recreates the context (new identity), leaving the old cache surface bound to a destroyed context.
        // Mirrors ServerCompositionVisualCache's context-affinity guard.
        var reallocated = false;
        if (_cache == null || _cacheSize != size || _cacheContext != renderContext)
        {
            _cache?.Dispose();
            _cache = canvas.CreateBackdropCache(size);
            _cacheSize = size;
            _cacheContext = renderContext;
            reallocated = true;
        }

        // A freshly allocated cache holds nothing usable, so refresh the whole AABB regardless of `pending`.
        PixelRect[] dirtyRects;
        if (reallocated)
        {
            _singleDirtyRect[0] = surfaceAabb.ToPixelRect();
            dirtyRects = _singleDirtyRect;
        }
        else if (pending is { } p && p.Right > p.Left && p.Bottom > p.Top)
        {
            _singleDirtyRect[0] = p.ToPixelRect();
            dirtyRects = _singleDirtyRect;
        }
        else
            dirtyRects = Array.Empty<PixelRect>();

        canvas.DrawRetainedBackdropEffect(_cache!, dirtyRects, effect, destRect);
    }
}
