// ReSharper disable once CheckNamespace

using System;
using Avalonia.Animation.Animators;

namespace Avalonia.Media;

public interface IDropShadowEffect : IEffect
{
    double OffsetX { get; }
    double OffsetY { get; }
    double BlurRadius { get; }
    Color Color { get; }
    double Opacity { get; }
}

internal interface IDirectionDropShadowEffect : IDropShadowEffect
{
    double Direction { get; }
    double ShadowDepth { get; }
}

public class ImmutableDropShadowEffect : IDropShadowEffect, IImmutableEffect
{
    static ImmutableDropShadowEffect()
    {
        EffectAnimator.EnsureRegistered();
    }
    
    public ImmutableDropShadowEffect(double offsetX, double offsetY, double blurRadius, Color color, double opacity)
    {
        OffsetX = offsetX;
        OffsetY = offsetY;
        BlurRadius = blurRadius;
        Color = color;
        Opacity = opacity;
    }

    public double OffsetX { get; }
    public double OffsetY { get; }
    public double BlurRadius { get; }
    public Color Color { get; }
    public double Opacity { get; }
    public bool Equals(IEffect? other)
    {
        return other is IDropShadowEffect d
               && d.OffsetX == OffsetX && d.OffsetY == OffsetY
               && d.BlurRadius == BlurRadius
               && d.Color == Color && d.Opacity == Opacity;
    }
}


public class ImmutableDropShadowDirectionEffect : IDirectionDropShadowEffect, IImmutableEffect
{
    static ImmutableDropShadowDirectionEffect()
    {
        EffectAnimator.EnsureRegistered();
    }
    
    public ImmutableDropShadowDirectionEffect(double direction, double shadowDepth, double blurRadius, Color color, double opacity)
    {
        Direction = direction;
        ShadowDepth = shadowDepth;
        BlurRadius = blurRadius;
        Color = color;
        Opacity = opacity;
    }
    
    public double OffsetX => Math.Cos(Direction * Math.PI / 180) * ShadowDepth;
    public double OffsetY => Math.Sin(Direction * Math.PI / 180) * ShadowDepth;
    public double Direction { get; }
    public double ShadowDepth { get; }
    public double BlurRadius { get; }
    public Color Color { get; }
    public double Opacity { get; }
    public bool Equals(IEffect? other)
    {
        return other is IDropShadowEffect d
               && d.OffsetX == OffsetX && d.OffsetY == OffsetY
               && d.BlurRadius == BlurRadius
               && d.Color == Color && d.Opacity == Opacity;
    }
}