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

    // A backdrop effect reads the backdrop only inside the backdrop bounds: the input is cropped to them, and a
    // blur sees the edge pixels repeated outside. The current opacity is not part of the filter: the callers
    // apply it when they draw the result.
    static SKImageFilter? CreateBackdropEffect(IEffect effect, SKRect bounds)
    {
        if (effect is IBlurEffect blur)
        {
            if (blur.Radius <= 0)
                return null;
            var sigma = SkBlurRadiusToSigma(blur.Radius);
            return SKImageFilter.CreateBlur(sigma, sigma, SKShaderTileMode.Clamp, null, bounds);
        }

        if (effect is IDropShadowEffect drop)
        {
            var sigma = drop.BlurRadius > 0 ? SkBlurRadiusToSigma(drop.BlurRadius) : 0;
            var alpha = (byte)Math.Max(0, Math.Min(255, drop.Color.A * drop.Opacity));
            var color = new SKColor(drop.Color.R, drop.Color.G, drop.Color.B, alpha);
            using var cropped = SKImageFilter.CreateOffset(0, 0, null, bounds);
            return SKImageFilter.CreateDropShadow((float)drop.OffsetX, (float)drop.OffsetY, sigma, sigma, color,
                cropped);
        }

        return null;
    }

    public bool SupportsBackdrop => Surface != null;

    // The retained path reads Surface, which is always the layer's own surface. While an intermediate layer
    // (effect, opacity mask, PushLayer, opacity) is open, drawing goes to that intermediate layer, so Surface does
    // not have the backdrop. Plain clip and transform saves do not open one.
    public bool IsBackdropSamplingSafe => Surface != null && _intermediateLayerDepth == 0;

    public IDrawingContextBackdropSnapshotImpl CreateBackdropSnapshot(PixelSize size)
    {
        CheckLease();
        return CreateRenderTarget(size, true, false);
    }

    public void DrawVolatileBackdropEffect(Rect bounds, IEffect effect)
    {
        CheckLease();
        // A SaveLayer with a backdrop filter starts with the filtered canvas contents (read through any open
        // intermediate layers). Restoring it right away draws that result back. A null filter (e.g. a zero blur
        // radius) gives an empty one, which draws nothing.
        // The SaveLayer bounds are only a size hint. Without the clip, filter output outside of them (e.g. a
        // drop-shadow offset) would be drawn, unlike CSS backdrop-filter, which clips to the border box.
        // The visual's content is drawn after the restore, so the clip does not apply to it.
        using var filter = CreateBackdropEffect(effect, bounds.ToSKRect());
        var paint = SKPaintCache.Shared.Get();
        // Applied when the SaveLayer is restored.
        paint.Color = new SKColor(255, 255, 255, (byte)(255 * _currentOpacity));
        var savedCount = Canvas.Save();
        Canvas.ClipRect(bounds.ToSKRect(), antialias: true);
        Canvas.SaveLayer(new SKCanvasSaveLayerRec { Bounds = bounds.ToSKRect(), Backdrop = filter, Paint = paint });
        SKPaintCache.Shared.ReturnReset(paint);
        // Restores the SaveLayer, which draws the filtered backdrop, and the clip.
        Canvas.RestoreToCount(savedCount);
    }

    public void DrawRetainedBackdropEffect(IDrawingContextBackdropSnapshotImpl snapshot, PixelRect snapshotBounds,
        IReadOnlyList<PixelRect> dirtyRects, IEffect effect, Rect bounds)
    {
        CheckLease();
        // The snapshot is always one that CreateBackdropSnapshot returned.
        if (Surface is not { } surface || snapshot is not SurfaceRenderTarget skiaSnapshot)
            return;

        // Layer pixel position of the snapshot's pixel (0,0).
        var origin = snapshotBounds.Position;

        // Refresh: copy the dirty rects from the layer's surface into the snapshot. Src blend replaces the old
        // snapshot pixels, also where the layer is not opaque.
        if (dirtyRects.Count > 0)
        {
            var snapshotCanvas = skiaSnapshot.Surface.Canvas;
            var srcPaint = SKPaintCache.Shared.Get();
            srcPaint.BlendMode = SKBlendMode.Src;
            var saved = snapshotCanvas.Save();
            snapshotCanvas.ResetMatrix();
            try
            {
                foreach (var dirty in dirtyRects)
                {
                    if (dirty.Width < 1 || dirty.Height < 1)
                        continue;
                    snapshotCanvas.Save();
                    snapshotCanvas.ClipRect(SKRect.Create(
                        dirty.X - origin.X, dirty.Y - origin.Y, dirty.Width, dirty.Height));
                    surface.Draw(snapshotCanvas, -origin.X, -origin.Y, srcPaint);
                    snapshotCanvas.Restore();
                }
            }
            finally
            {
                snapshotCanvas.RestoreToCount(saved);
                SKPaintCache.Shared.ReturnReset(srcPaint);
            }
        }

        // Draw the snapshot through the effect, clipped to the backdrop bounds. The shader matrix maps snapshot
        // pixels into the current coordinate space, so the snapshot stays aligned to layer pixels under any
        // transform, and only the clip follows the transform.
        using var image = skiaSnapshot.SnapshotImage();
        if (!(Transform * Matrix.CreateTranslation(-origin.X, -origin.Y)).TryInvert(out var localMatrix))
            return;

        using var filter = CreateBackdropEffect(effect, bounds.ToSKRect()); // null filter: plain copy
        using var shader = SKShader.CreateImage(
            image, SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, localMatrix.ToSKMatrix());

        var paint = SKPaintCache.Shared.Get();
        var savedCount = Canvas.Save();
        try
        {
            // Filter output outside of the bounds (e.g. a drop-shadow offset) is not drawn, same as in the
            // volatile path.
            var skBounds = bounds.ToSKRect();
            Canvas.ClipRect(skBounds, antialias: true);
            paint.Shader = shader;
            paint.ImageFilter = filter;
            paint.Color = new SKColor(255, 255, 255, (byte)(255 * _currentOpacity));
            Canvas.DrawRect(skBounds, paint);
        }
        finally
        {
            Canvas.RestoreToCount(savedCount);
            SKPaintCache.Shared.ReturnReset(paint);
        }
    }
}
