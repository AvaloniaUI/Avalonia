using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia;

partial class DrawingContextImpl
{

    public void PushEffect(Rect? effectClipRect, IEffect effect)
    {
        CheckLease();
        using var filter = CreateEffect(effect);
        var paint = SKPaintCache.Shared.Get();
        paint.ImageFilter = filter;
        if (effectClipRect.HasValue)
            Canvas.SaveLayer(effectClipRect.Value.ToSKRect(), paint);
        else
            Canvas.SaveLayer(paint);
        _intermediateLayerDepth++;
        SKPaintCache.Shared.ReturnReset(paint);
    }

    public void PopEffect()
    {
        CheckLease();
        RestoreCanvas();
        _intermediateLayerDepth--;
    }

    SKImageFilter? CreateEffect(IEffect effect)
    {
        if (effect is IBlurEffect blur)
        {
            if (blur.Radius <= 0)
                return null;
            var sigma = SkBlurRadiusToSigma(blur.Radius);
            return SKImageFilter.CreateBlur(sigma, sigma);
        }

        if (effect is IDropShadowEffect drop)
        {
            var sigma = drop.BlurRadius > 0 ? SkBlurRadiusToSigma(drop.BlurRadius) : 0;
            var alpha = drop.Color.A * drop.Opacity;
            if (!_useOpacitySaveLayer)
                alpha *= _currentOpacity;
            var color = new SKColor(drop.Color.R, drop.Color.G, drop.Color.B, (byte)Math.Max(0, Math.Min(255, alpha)));

            return SKImageFilter.CreateDropShadow((float)drop.OffsetX, (float)drop.OffsetY, sigma, sigma, color);
        }

        return null;
    }

    public bool SupportsBackdrop => Surface != null;

    // Safe unless an intermediate save-layer (Effect/OpacityMask/PushLayer/opacity save-layer) is active: those
    // redirect drawing to an offscreen while Surface still refers to the root, so a snapshot would read stale
    // pixels. Plain clip/transform saves don't affect the root surface and aren't counted.
    public bool IsBackdropSamplingSafe => Surface != null && _intermediateLayerDepth == 0;

    // Same creation path as CreateLayer; the returned SurfaceRenderTarget is the opaque cache handle.
    public IDrawingContextBackdropCacheImpl CreateBackdropCache(PixelSize size)
    {
        CheckLease();
        return CreateRenderTarget(size, true, false);
    }

    public void PushBackdropLayer(Rect bounds, IEffect effect)
    {
        CheckLease();
        // SaveLayerRec.Backdrop initialises the new layer with the current canvas contents (through any nested
        // save-layers) filtered by `filter`. A null filter (identity, e.g. R=0 blur) yields an empty layer,
        // which composites back as a no-op backdrop. SaveLayer consumes the filter synchronously, so the
        // `using` may dispose it once the call returns (mirrors PushEffect's paint-filter lifetime).
        using var filter = CreateEffect(effect);
        Canvas.SaveLayer(new SKCanvasSaveLayerRec { Bounds = bounds.ToSKRect(), Backdrop = filter });
        _intermediateLayerDepth++;
    }

    public void PopBackdropLayer()
    {
        CheckLease();
        RestoreCanvas();
        _intermediateLayerDepth--;
    }

    public void DrawRetainedBackdropEffect(IDrawingContextBackdropCacheImpl cache, IReadOnlyList<PixelRect> dirtyRects,
        IEffect effect, Rect destRect)
    {
        CheckLease();
        // The backend only ever hands back a cache it created via CreateBackdropCache, so this cast is safe.
        if (Surface is not { } surface || cache is not SurfaceRenderTarget skiaLayer)
            return;

        // The layer's pixel (0,0) sits at this device-space position on the target surface.
        var layerOrigin = PixelRect.FromRect(destRect.TransformToAABB(Transform), 1).Position;
        var layerSurface = skiaLayer.Surface;

        // Step 1: refresh the damaged regions of the layer directly from the live target. A single
        // SKSurface.Draw blits the whole target onto the layer canvas at (-layerOrigin); the per-rect clip
        // restricts each write to one dirty region. Src blend overwrites, so sub-alpha target pixels replace
        // the stale layer pixels instead of compositing over them.
        if (dirtyRects.Count > 0)
        {
            var layerCanvas = layerSurface.Canvas;
            var srcPaint = SKPaintCache.Shared.Get();
            srcPaint.BlendMode = SKBlendMode.Src;
            var saved = layerCanvas.Save();
            layerCanvas.ResetMatrix();
            try
            {
                foreach (var dirty in dirtyRects)
                {
                    if (dirty.Width < 1 || dirty.Height < 1)
                        continue;
                    layerCanvas.Save();
                    layerCanvas.ClipRect(SKRect.Create(
                        dirty.X - layerOrigin.X, dirty.Y - layerOrigin.Y, dirty.Width, dirty.Height));
                    surface.Draw(layerCanvas, -layerOrigin.X, -layerOrigin.Y, srcPaint);
                    layerCanvas.Restore();
                }
            }
            finally
            {
                layerCanvas.RestoreToCount(saved);
                SKPaintCache.Shared.ReturnReset(srcPaint);
            }
        }

        // Step 2: draw the (now current) layer through the effect at destRect. The local matrix maps layer
        // pixels back into the current coordinate space.
        using var image = skiaLayer.SnapshotImage();
        if (!(Transform * Matrix.CreateTranslation(-layerOrigin.X, -layerOrigin.Y)).TryInvert(out var localMatrix))
            return;

        using var filter = CreateEffect(effect); // null filter -> identity blit
        using var shader = SKShader.CreateImage(
            image, SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, localMatrix.ToSKMatrix());

        var paint = SKPaintCache.Shared.Get();
        try
        {
            paint.Shader = shader;
            paint.ImageFilter = filter;
            paint.Color = new SKColor(255, 255, 255, (byte)(255 * _currentOpacity));
            Canvas.DrawRect(destRect.ToSKRect(), paint);
        }
        finally
        {
            SKPaintCache.Shared.ReturnReset(paint);
        }
    }
}
