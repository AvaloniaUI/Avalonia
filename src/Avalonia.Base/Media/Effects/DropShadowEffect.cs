// ReSharper disable once CheckNamespace

using System;
// ReSharper disable CheckNamespace

namespace Avalonia.Media;

public abstract class DropShadowEffectBase : Effect
{
    public static readonly StyledProperty<double> BlurRadiusProperty =
        AvaloniaProperty.Register<DropShadowEffectBase, double>(
            nameof(BlurRadius), 5);

    public double BlurRadius
    {
        get => GetValue(BlurRadiusProperty);
        set => SetValue(BlurRadiusProperty, value);
    }

    public static readonly StyledProperty<Color> ColorProperty = AvaloniaProperty.Register<DropShadowEffectBase, Color>(
        nameof(Color), Colors.Black);

    public Color Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public static readonly StyledProperty<double> OpacityProperty =
        AvaloniaProperty.Register<DropShadowEffectBase, double>(
            nameof(Opacity), 1);

    public double Opacity
    {
        get => GetValue(OpacityProperty);
        set => SetValue(OpacityProperty, value);
    }

    static DropShadowEffectBase()
    {
        AffectsRender<DropShadowEffectBase>(BlurRadiusProperty, ColorProperty, OpacityProperty);
    }
}

public sealed class DropShadowEffect : DropShadowEffectBase, IDropShadowEffect, IMutableEffect
{
    public static readonly StyledProperty<double> OffsetXProperty = AvaloniaProperty.Register<DropShadowEffect, double>(
        nameof(OffsetX), 3.5355);

    public double OffsetX
    {
        get => GetValue(OffsetXProperty);
        set => SetValue(OffsetXProperty, value);
    }
    
    public static readonly StyledProperty<double> OffsetYProperty = AvaloniaProperty.Register<DropShadowEffect, double>(
        nameof(OffsetY), 3.5355);

    public double OffsetY
    {
        get => GetValue(OffsetYProperty);
        set => SetValue(OffsetYProperty, value);
    }

    static DropShadowEffect()
    {
        AffectsRender<DropShadowEffect>(OffsetXProperty, OffsetYProperty);
    }

    public IImmutableEffect ToImmutable()
    {
        return new ImmutableDropShadowEffect(OffsetX, OffsetY, BlurRadius, Color, Opacity);
    }
}

/// <summary>
/// This class is compatible with WPF's DropShadowEffect and provides Direction and ShadowDepth properties instead of OffsetX/OffsetY
/// </summary>
public sealed class DropShadowDirectionEffect : DropShadowEffectBase, IDirectionDropShadowEffect, IMutableEffect
{
    public static readonly StyledProperty<double> ShadowDepthProperty =
        AvaloniaProperty.Register<DropShadowDirectionEffect, double>(
            nameof(ShadowDepth), 5);

    public double ShadowDepth
    {
        get => GetValue(ShadowDepthProperty);
        set => SetValue(ShadowDepthProperty, value);
    }

    public static readonly StyledProperty<double> DirectionProperty = AvaloniaProperty.Register<DropShadowDirectionEffect, double>(
        nameof(Direction), 315);

    public double Direction
    {
        get => GetValue(DirectionProperty);
        set => SetValue(DirectionProperty, value);
    }
    
    public double OffsetX => Math.Cos(Direction * Math.PI / 180) * ShadowDepth;
    public double OffsetY => Math.Sin(Direction * Math.PI / 180) * ShadowDepth;
    
    public IImmutableEffect ToImmutable() => new ImmutableDropShadowDirectionEffect(OffsetX, OffsetY, BlurRadius, Color, Opacity);
}