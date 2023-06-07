using System.Numerics;
using MicroCom.Runtime;

namespace Avalonia.Win32.WinRT.Composition;

internal static class WinUiCompositionUtils
{
    public static ICompositionBrush? CreateMicaBackdropBrush(ICompositor compositor)
    {
        if (Win32Platform.WindowsVersion.Build < 22000)
            return null;

        using var compositorWithBlurredWallpaperBackdropBrush =
            compositor.QueryInterface<ICompositorWithBlurredWallpaperBackdropBrush>();
        using var blurredWallpaperBackdropBrush =
            compositorWithBlurredWallpaperBackdropBrush?.TryCreateBlurredWallpaperBackdropBrush();
        return blurredWallpaperBackdropBrush?.QueryInterface<ICompositionBrush>();
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
}
