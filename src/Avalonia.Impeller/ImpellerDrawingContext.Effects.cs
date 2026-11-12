using System;
using System.Collections.Generic;
using Avalonia.Impeller.Interop;
using Avalonia.Media;
using NImpeller;

namespace Avalonia.Impeller;

partial class ImpellerDrawingContext
{
    // One entry per PushEffect; invoked (LIFO) by PopEffect to unwind the effect it started.
    private readonly Stack<Action> _effectPops = new();

    public void PushEffect(Rect? effectClipRect, IEffect effect)
    {
        var bounds = (effectClipRect ?? FullBounds).ToImpeller();

        if (effect is IDropShadowEffect drop)
        {
            PushDropShadow(drop, bounds);
            return;
        }

        using var paint = ImpellerPaint.New()!;
        if (effect is IBlurEffect { Radius: > 0 } blur)
        {
            var sigma = SkBlurRadiusToSigma(blur.Radius * DeviceScale);
            using var filter = ImpellerImageFilter.CreateBlurNew(sigma, sigma, ImpellerTileMode.kImpellerTileModeDecal);
            if (filter is not null)
                paint.SetImageFilter(filter);
        }

        _builder.SaveLayer(bounds, paint);
        _effectPops.Push(_restoreBuilder ??= () => _builder.Restore());
    }

    public void PopEffect()
    {
        if (_effectPops.Count == 0)
        {
            _builder.Restore();
            return;
        }

        _effectPops.Pop().Invoke();
    }

    private Action? _restoreBuilder;

    private void PushDropShadow(IDropShadowEffect drop, ImpellerRect bounds)
    {
        var cull = new ImpellerRect { X = 0, Y = 0, Width = _size.Width, Height = _size.Height };
        var sub = ImpellerDisplayListBuilder.New(cull);
        if (sub is null)
        {
            using var paint = ImpellerPaint.New()!;
            _builder.SaveLayer(bounds, paint);
            _effectPops.Push(_restoreBuilder ??= () => _builder.Restore());
            return;
        }

        var parent = _builder;
        var transformAtPush = _currentTransform;
        _builder = sub;
        _builder.SetTransform((_currentTransform * _baseTransform).ToImpeller());

        _effectPops.Push(() => PopDropShadow(drop, parent, sub, transformAtPush));
    }

    private void PopDropShadow(IDropShadowEffect drop,
        ImpellerDisplayListBuilder parent, ImpellerDisplayListBuilder sub, Matrix transformAtPush)
    {
        _builder = parent;
        using var content = sub.CreateDisplayListNew();
        sub.Dispose();
        if (content is null)
            return;

        var deviceBounds = new ImpellerRect { X = 0, Y = 0, Width = _size.Width, Height = _size.Height };
        _builder.Save();
        _builder.SetTransform(Matrix.Identity.ToImpeller());

        var lin = transformAtPush * _baseTransform;
        var devDx = (float)(drop.OffsetX * lin.M11 + drop.OffsetY * lin.M21);
        var devDy = (float)(drop.OffsetX * lin.M12 + drop.OffsetY * lin.M22);
        var scale = 0.5 * (Math.Sqrt(lin.M11 * lin.M11 + lin.M12 * lin.M12)
                           + Math.Sqrt(lin.M21 * lin.M21 + lin.M22 * lin.M22));
        var sigma = SkBlurRadiusToSigma(drop.BlurRadius * scale);

        var alpha = Math.Clamp(drop.Color.A / 255.0 * drop.Opacity, 0, 1);
        var tint = new ImpellerColor
        {
            Alpha = (float)alpha,
            Red = drop.Color.R / 255f,
            Green = drop.Color.G / 255f,
            Blue = drop.Color.B / 255f,
            Color_space = ImpellerColorSpace.kImpellerColorSpaceSRGB
        };

        // Recolor and blur the content, offset by translating it (a bare matrix image filter crashes Impeller).
        using (var shadowPaint = ImpellerPaint.New()!)
        using (var colorFilter = ImpellerColorFilter.CreateBlendNew(tint, ImpellerBlendMode.kImpellerBlendModeSourceIn))
        using (var blur = sigma > 0 ? ImpellerImageFilter.CreateBlurNew(sigma, sigma, ImpellerTileMode.kImpellerTileModeDecal) : null)
        {
            if (colorFilter is not null)
                shadowPaint.SetColorFilter(colorFilter);
            if (blur is not null)
                shadowPaint.SetImageFilter(blur);

            _builder.SaveLayer(deviceBounds, shadowPaint);
            _builder.Save();
            _builder.SetTransform(Matrix.CreateTranslation(devDx, devDy).ToImpeller());
            _builder.DrawDisplayList(content, 1.0f);
            _builder.Restore();
            _builder.Restore();
        }

        _builder.DrawDisplayList(content, 1.0f);
        _builder.Restore();
    }

    private double DeviceScale
    {
        get
        {
            var m = _currentTransform * _baseTransform;
            var sx = Math.Sqrt(m.M11 * m.M11 + m.M12 * m.M12);
            var sy = Math.Sqrt(m.M21 * m.M21 + m.M22 * m.M22);
            return Math.Max(0.5 * (sx + sy), 0.01);
        }
    }

    private static float SkBlurRadiusToSigma(double radius) =>
        radius <= 0 ? 0f : 0.288675f * (float)radius + 0.5f;
}
