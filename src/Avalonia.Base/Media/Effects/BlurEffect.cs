using System;
// ReSharper disable CheckNamespace
namespace Avalonia.Media;

public sealed class BlurEffect : Effect, IBlurEffect, IMutableEffect
{
    public static readonly StyledProperty<double> RadiusProperty = AvaloniaProperty.Register<BlurEffect, double>(
        nameof(Radius), 5);

    public double Radius
    {
        get => GetValue(RadiusProperty);
        set => SetValue(RadiusProperty, value);
    }
    
    static BlurEffect()
    {
        AffectsRender<BlurEffect>(RadiusProperty);
    }

    public IImmutableEffect ToImmutable() => new ImmutableBlurEffect(Radius);
}