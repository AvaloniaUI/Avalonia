using System;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Server;

internal partial class ServerCompositionBitmapCache
{
    private bool _needsFullReRender;
    private IDrawingContextLayerImpl? _layer;
    private IPlatformRenderInterfaceContext? _layerCreatedWithContext;
    private bool _layerHasTextAntialiasing;
    private PixelSize _desiredLayerSize;
    private double _scaleX, _scaleY;
    private Vector _drawAtOffset;
    private bool _needToFinalizeFrame = true;

    public IDirtyRectCollector DirtyRectCollector { get; private set; } = null!;
    public bool IsDirty => !_dirtyRectTracker.IsEmpty;

    partial void Initialize()
    {
        DirtyRectCollector = new DirtyRectCollectorProxy(this);
        MarkForFullReRender();
    }

    public override void FreeResources()
    {
        _layer?.Dispose();
        _layerCreatedWithContext = null;
    }

    protected override void ValuesInvalidated()
    {
        var targetVisual = TargetVisual;
        if (targetVisual?.Root == null)
            return;

        MarkForFullReRender();
        targetVisual.OnCacheModeStateChanged();
        base.ValuesInvalidated();
    }
    
    void ResetDirtyRects()
    {
        _needToFinalizeFrame = true;
        _dirtyRectTracker.Initialize(LtrbRect.Infinite);
    }
    
    void MarkForFullReRender()
    {
        _needsFullReRender = true;
        ResetDirtyRects();
    }

    class DirtyRectCollectorProxy(ServerCompositionBitmapCache parent) : IDirtyRectCollector
    {
        public void AddRect(LtrbRect rect)
        {
            parent._needToFinalizeFrame = true;
            
            // scale according to our render transform, since those values come in local space of the visual
            parent._dirtyRectTracker.AddRect(new LtrbRect((rect.Left + parent._drawAtOffset.X) * parent._scaleX,
                (rect.Top + parent._drawAtOffset.Y) * parent._scaleY,
                (rect.Right + parent._drawAtOffset.X) * parent._scaleX,
                (rect.Bottom + parent._drawAtOffset.Y) * parent._scaleY));
        }
    }
    
    private readonly IDirtyRectTracker _dirtyRectTracker = new SingleDirtyRectTracker();

    static bool IsCloseReal(double a, double b)
    {
        // Underlying rendering platform is using floats anyway, so we use float epsilon here
        return (Math.Abs((a - b) / ((b == 0.0f) ? 1.0f : b)) < 10.0f * MathUtilities.FloatEpsilon); 
    }
    
    
    bool UpdateRealizationDimensions()
    {
        var targetVisual = TargetVisual;
        if(!(targetVisual is { Root: not null, SubTreeBounds: {} visualBounds }))
            return false;

        // Since the cache relies only on local space bounds, the DPI isn't taken into account (as it's the root
        // transform of the visual tree).  Scale for DPI if needed here.
        var scale = targetVisual.Root.Scaling * RenderAtScale;
        
        
            
        // Caches are not clipped to the window bounds, they use local space bounds,
        // so (especially in combination with RenderScale) a very large intermediate
        // surface could be requested.  Instead of failing in this case, we clamp the 
        // surface to the max texture size, which can cause some pixelation but will 
        // allow the app to render in hardware and still benefit from a cache.
        var maxSize = Compositor.RenderInterface.Value.MaxOffscreenRenderTargetPixelSize
                             ?? new PixelSize(16384, 16384);

        // We round our bounds up to integral values for consistency here, since we need to do so when creating the surface anyway.
        // This also ensures that our content will always be drawn in its entirety in the texture.
        //  Future Consideration:  Note that if we want to use the cache texture for TextureBrush or as input to Effects, we'll
        //          need to be able to toggle this "snap-out" behavior to avoid seams since Effects by default
        //          do NOT snap the size out, they round down to integral bounds.
        var fWidth = visualBounds.Width * scale;
        var uWidth = (int)fWidth;
        // If our width was non-integer, round up.
        if (!IsCloseReal(fWidth, fWidth))
            uWidth++;

        var fHeight = visualBounds.Height * scale;
        var uHeight = (int)fHeight;
        
        // If our height was non-integer, round up.
        if (!IsCloseReal(fHeight, fHeight))
            uHeight++;

        _scaleX = _scaleY = scale;
        if (uWidth > maxSize.Width)
        {
            _scaleX *= (double)maxSize.Width / uWidth;
            uWidth = maxSize.Width;
        }
        
        if(uHeight > maxSize.Height)
        {
            _scaleY *= (double)maxSize.Height / uHeight;
            uHeight = maxSize.Height;
        }

        _drawAtOffset = new Vector(-visualBounds.Left, -visualBounds.Top);

        _desiredLayerSize = new PixelSize(uWidth, uHeight);
        
        return true;
    }
    
    public (int visitedVisuals, int renderedVisuals) Draw(IDrawingContextImpl outerCanvas)
    {
        if (TargetVisual == null)
            return default;

        if (TargetVisual.SubTreeBounds == null)
            return default;
        
        UpdateRealizationDimensions();

        var renderContext = Compositor.RenderInterface.Value;

        // Re-create layer if needed
        if (_layer == null
            || _layerHasTextAntialiasing != EnableClearType
            || _layer.PixelSize != _desiredLayerSize
            || _layerCreatedWithContext != renderContext)
        {
            _layer?.Dispose();
            _layer = null;
            _layerCreatedWithContext = null;

            if (_desiredLayerSize.Width < 1 || _desiredLayerSize.Height < 1)
            {
                ResetDirtyRects();
                return default;
            }
            
            _layer = renderContext.CreateOffscreenRenderTarget(_desiredLayerSize, new Vector(_scaleX, _scaleX),
                EnableClearType);
            _layerHasTextAntialiasing = EnableClearType;
            _layerCreatedWithContext = renderContext;
            _needsFullReRender = true;
        }

        var fullFrameRect = new LtrbRect(0, 0,
            _layer.PixelSize.Width, _layer.PixelSize.Height);

        // Extend the dirty rect area if needed
        if (_needsFullReRender)
        {
            ResetDirtyRects();
            DirtyRectCollector.AddRect(LtrbRect.Infinite);
        }

        // Compute the final dirty rect set that accounts for antialiasing effects
        if (_needToFinalizeFrame)
        {
            _dirtyRectTracker.FinalizeFrame(fullFrameRect);
            _needToFinalizeFrame = false;
        }

        var visualLocalBounds = TargetVisual.SubTreeBounds.Value;
        (int, int) rv = default;
        // Render to layer if needed
        if (!_dirtyRectTracker.IsEmpty)
        {
            using var ctx = _layer.CreateDrawingContext(false);
            using (_needsFullReRender ? null : _dirtyRectTracker.BeginDraw(ctx))
            {
                ctx.Clear(Colors.Transparent);
                ctx.Transform = Matrix.CreateTranslation(_drawAtOffset) * Matrix.CreateScale(_scaleX, _scaleY);
                rv = TargetVisual.Render(ctx, _dirtyRectTracker.CombinedRect, _dirtyRectTracker, renderingToBitmapCache: true);
            }
        }
        _needsFullReRender = false;

        var renderBitmapAtBounds = TargetVisual.SubTreeBounds.Value.ToRect();
        var originalTransform = outerCanvas.Transform;
        if (SnapsToDevicePixels)
        {
            var worldBounds = renderBitmapAtBounds.TransformToAABB(originalTransform);
            var snapOffsetX = worldBounds.Left - Math.Floor(worldBounds.Left);
            var snapOffsetY = worldBounds.Top - Math.Floor(worldBounds.Top);
            outerCanvas.Transform = originalTransform * Matrix.CreateTranslation(-snapOffsetX, -snapOffsetY);
        }
        
        //TODO: Maybe adjust for that extra pixel added due to rounding?
        outerCanvas.DrawBitmap(_layer, 1, new Rect(0,0, _layer.PixelSize.Width, _layer.PixelSize.Height),
            TargetVisual.SubTreeBounds.Value.ToRect());
        if (SnapsToDevicePixels)
            outerCanvas.Transform = originalTransform;
        
        // Set empty dirty rects for next frame
        ResetDirtyRects();
        return rv;
    }
}