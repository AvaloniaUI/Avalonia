using System;
using System.Numerics;
using MicroCom.Runtime;

namespace Avalonia.Win32.WinRT.Composition;

internal static class WinUiCompositionUtils
{
    public static ICompositionBrush? CreateMicaBackdropBrush(ICompositor compositor, float color, float opacity)
    {
        if (Win32Platform.WindowsVersion.Build < 22000)
            return null;

        using var backDropParameterFactory =
            NativeWinRTMethods.CreateActivationFactory<ICompositionEffectSourceParameterFactory>(
                "Windows.UI.Composition.CompositionEffectSourceParameter");


        var tint = new[] { color / 255f, color / 255f, color / 255f, 255f / 255f };

        using var tintColorEffect = new ColorSourceEffect(tint);


        using var tintOpacityEffect = new OpacityEffect(1.0f, tintColorEffect);
        using var tintOpacityEffectFactory = compositor.CreateEffectFactory(tintOpacityEffect);
        using var tintOpacityEffectBrushEffect = tintOpacityEffectFactory.CreateBrush();
        using var tintOpacityEffectBrush = tintOpacityEffectBrushEffect.QueryInterface<ICompositionBrush>();

        using var luminosityColorEffect = new ColorSourceEffect(tint);

        using var luminosityOpacityEffect = new OpacityEffect(opacity, luminosityColorEffect);
        using var luminosityOpacityEffectFactory = compositor.CreateEffectFactory(luminosityOpacityEffect);
        using var luminosityOpacityEffectBrushEffect = luminosityOpacityEffectFactory.CreateBrush();
        using var luminosityOpacityEffectBrush =
            luminosityOpacityEffectBrushEffect.QueryInterface<ICompositionBrush>();
        
        using var compositorWithBlurredWallpaperBackdropBrush =
            compositor.QueryInterface<ICompositorWithBlurredWallpaperBackdropBrush>();
        using var blurredWallpaperBackdropBrush =
            compositorWithBlurredWallpaperBackdropBrush?.TryCreateBlurredWallpaperBackdropBrush();
        using var micaBackdropBrush = blurredWallpaperBackdropBrush?.QueryInterface<ICompositionBrush>();


        using var backgroundParameterAsSource =
            GetParameterSource("Background", backDropParameterFactory, out var backgroundHandle);
        using var foregroundParameterAsSource =
            GetParameterSource("Foreground", backDropParameterFactory, out var foregroundHandle);

        using var luminosityBlendEffect =
            new BlendEffect(23, backgroundParameterAsSource, foregroundParameterAsSource);
        using var luminosityBlendEffectFactory = compositor.CreateEffectFactory(luminosityBlendEffect);
        using var luminosityBlendEffectBrush = luminosityBlendEffectFactory.CreateBrush();
        using var luminosityBlendEffectBrush1 = luminosityBlendEffectBrush.QueryInterface<ICompositionBrush>();
        luminosityBlendEffectBrush.SetSourceParameter(backgroundHandle, micaBackdropBrush);
        luminosityBlendEffectBrush.SetSourceParameter(foregroundHandle, luminosityOpacityEffectBrush);


        using var backgroundParameterAsSource1 =
            GetParameterSource("Background", backDropParameterFactory, out var backgroundHandle1);
        using var foregroundParameterAsSource1 =
            GetParameterSource("Foreground", backDropParameterFactory, out var foregroundHandle1);

        using var colorBlendEffect =
            new BlendEffect(22, backgroundParameterAsSource1, foregroundParameterAsSource1);
        using var colorBlendEffectFactory = compositor.CreateEffectFactory(colorBlendEffect);
        using var colorBlendEffectBrush = colorBlendEffectFactory.CreateBrush();
        colorBlendEffectBrush.SetSourceParameter(backgroundHandle1, luminosityBlendEffectBrush1);
        colorBlendEffectBrush.SetSourceParameter(foregroundHandle1, tintOpacityEffectBrush);


        // colorBlendEffectBrush.SetSourceParameter(backgroundHandle, micaBackdropBrush);

        using var micaBackdropBrush1 = colorBlendEffectBrush.QueryInterface<ICompositionBrush>();
        return micaBackdropBrush1.CloneReference();
    }

    public static ICompositionBrush CreateAcrylicBlurBackdropBrush(ICompositor compositor)
    {
        using var backDropParameterFactory =
            NativeWinRTMethods.CreateActivationFactory<ICompositionEffectSourceParameterFactory>(
                "Windows.UI.Composition.CompositionEffectSourceParameter");
        using var backdropString = new HStringInterop("backdrop");
        using var backDropParameter =
            backDropParameterFactory.Create(backdropString.Handle);
        using var backDropParameterAsSource = backDropParameter.QueryInterface<IGraphicsEffectSource>();
        var blurEffect = new WinUIGaussianBlurEffect(backDropParameterAsSource);
        using var blurEffectFactory = compositor.CreateEffectFactory(blurEffect);
        using var compositionEffectBrush = blurEffectFactory.CreateBrush();
        using var backdropBrush = CreateBackdropBrush(compositor);

        var saturateEffect = new SaturationEffect(blurEffect);
        using var satEffectFactory = compositor.CreateEffectFactory(saturateEffect);
        using var sat = satEffectFactory.CreateBrush();
        compositionEffectBrush.SetSourceParameter(backdropString.Handle, backdropBrush);
        return compositionEffectBrush.QueryInterface<ICompositionBrush>();
    }

    public static ICompositionRoundedRectangleGeometry? ClipVisual(ICompositor compositor, float? _backdropCornerRadius,  params IVisual?[] containerVisuals)
    {
        if (!_backdropCornerRadius.HasValue)
            return null;
        using var compositor5 = compositor.QueryInterface<ICompositor5>();
        using var roundedRectangleGeometry = compositor5.CreateRoundedRectangleGeometry();
        roundedRectangleGeometry.SetCornerRadius(new Vector2(_backdropCornerRadius.Value, _backdropCornerRadius.Value));

        using var compositor6 = compositor.QueryInterface<ICompositor6>();
        using var compositionGeometry = roundedRectangleGeometry
            .QueryInterface<ICompositionGeometry>();

        using var geometricClipWithGeometry =
            compositor6.CreateGeometricClipWithGeometry(compositionGeometry);
        foreach (var visual in containerVisuals)
        {
            visual?.SetClip(geometricClipWithGeometry.QueryInterface<ICompositionClip>());
        }

        return roundedRectangleGeometry.CloneReference();
    }

    public static IVisual CreateBlurVisual(ICompositor compositor, ICompositionBrush compositionBrush)
    {
        using var spriteVisual = compositor.CreateSpriteVisual();
        using var visual = spriteVisual.QueryInterface<IVisual>();
        using var visual2 = spriteVisual.QueryInterface<IVisual2>();


        spriteVisual.SetBrush(compositionBrush);
        visual.SetIsVisible(0);
        visual2.SetRelativeSizeAdjustment(new Vector2(1.0f, 1.0f));

        return visual.CloneReference();
    }

    public static ICompositionBrush CreateBackdropBrush(ICompositor compositor)
    {
        ICompositionBackdropBrush? brush = null;
        try
        {
            if (Win32Platform.WindowsVersion >= WinUiCompositionShared.MinHostBackdropVersion)
            {
                using var compositor3 = compositor.QueryInterface<ICompositor3>();
                brush = compositor3.CreateHostBackdropBrush();
            }
            else
            {
                using var compositor2 = compositor.QueryInterface<ICompositor2>();
                brush = compositor2.CreateBackdropBrush();
            }

            return brush.QueryInterface<ICompositionBrush>();
        }
        finally
        {
            brush?.Dispose();
        }
    }
    
    private static IGraphicsEffectSource GetParameterSource(string name,
        ICompositionEffectSourceParameterFactory backDropParameterFactory, out IntPtr handle)
    {
        var backdropString = new HStringInterop(name);
        var backDropParameter =
            backDropParameterFactory.Create(backdropString.Handle);
        var backDropParameterAsSource = backDropParameter.QueryInterface<IGraphicsEffectSource>();
        handle = backdropString.Handle;
        return backDropParameterAsSource;
    }
}
