using System;
using Avalonia.Media;
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
        SKPaintCache.Shared.ReturnReset(paint);
    }

    public void DrawBackdropEffect(Rect rect, IEffect effect)
    {
        if (Surface == null) return;

        var sourceSnapshotRect = PixelRect.FromRect(rect.TransformToAABB(Transform), 1);
        if (sourceSnapshotRect.Width == 0 || sourceSnapshotRect.Height == 0)
            return;

        var transform = Transform * Matrix.CreateTranslation(-sourceSnapshotRect.X, -sourceSnapshotRect.Y);
        
        if (!transform.TryInvert(out var matrix))
            return;
            
        Canvas.Flush();

        using var filter = CreateEffect(effect);
        if (filter == null)
            return;

        using var snapshot = Surface.Snapshot(sourceSnapshotRect.ToSKRectI());
             
            
        //matrix = matrix * Matrix.CreateTranslation(-sourceSnapshotRect.X, -sourceSnapshotRect.Y);
            
            
        using var sourceShader =SKShader.CreateImage(snapshot, SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, matrix.ToSKMatrix());

        var paint = SKPaintCache.Shared.Get();
        try
        {
            paint.Shader = sourceShader;
            paint.ImageFilter = filter;
            Canvas.DrawRect(rect.ToSKRect(), paint);
        }
        finally
        {
            SKPaintCache.Shared.ReturnReset(paint);
        }

        Canvas.Flush();
    }

    public void PopEffect()
    {
        CheckLease();
        Canvas.Restore();
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
    
}
