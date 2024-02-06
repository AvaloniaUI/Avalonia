using System;
using System.Diagnostics.CodeAnalysis;
using SkiaSharp;

namespace Avalonia.Skia;

internal static partial class SkiaCompat
{
    private static Func<float, float, SKImageFilter>? s_sk3FilterBlurFactory; 
    private static Func<float, float, float, float, SKColor, SKImageFilter>? s_sk3FilterDropShadowFactory; 

    public static SKImageFilter CreateBlur(float sigmaX, float sigmaY)
    {
        if (IsSkiaSharp3) return NewCall(sigmaX, sigmaY);
        else return LegacyCall(sigmaX, sigmaY);

        static SKImageFilter LegacyCall(float sigmaX, float sigmaY) => SKImageFilter.CreateBlur(sigmaX, sigmaY);

        [DynamicDependency("CreateBlur(System.Single,System.Single)", typeof(SKImageFilter))]
        static SKImageFilter NewCall(float sigmaX, float sigmaY)
        {
            if (s_sk3FilterBlurFactory is null)
            {
                var method = typeof(SKImageFilter).GetMethod("CreateBlur", new[] { typeof(float), typeof(float) })!;
                s_sk3FilterBlurFactory = (Func<float, float, SKImageFilter>)Delegate.CreateDelegate(typeof(Func<float, float, SKImageFilter>), null, method);
            }

            return s_sk3FilterBlurFactory(sigmaX, sigmaY);
        }
    }

    public static SKImageFilter CreateDropShadow(float dropOffsetX, float dropOffsetY, float sigma, float f, SKColor color)
    {
        if (IsSkiaSharp3) return NewCall(dropOffsetX, dropOffsetY, sigma, f, color);
        else return LegacyCall(dropOffsetX, dropOffsetY, sigma, f, color);

        static SKImageFilter LegacyCall(float dropOffsetX, float dropOffsetY, float sigma, float f, SKColor color)
            => SKImageFilter.CreateDropShadow(dropOffsetX, dropOffsetY, sigma, f, color);

        [DynamicDependency("CreateDropShadow(System.Single,System.Single,System.Single,System.Single,SkiaSharp.SKColor)", typeof(SKImageFilter))]
        static SKImageFilter NewCall(float dropOffsetX, float dropOffsetY, float sigma, float f, SKColor color)
        {
            if (s_sk3FilterDropShadowFactory is null)
            {
                var method = typeof(SKImageFilter).GetMethod("CreateDropShadow",
                    new[] { typeof(float), typeof(float), typeof(float), typeof(float), typeof(SKColor) })!;
                s_sk3FilterDropShadowFactory = (Func<float, float, float, float, SKColor, SKImageFilter>)
                    Delegate.CreateDelegate(typeof(Func<float, float, float, float, SKColor, SKImageFilter>), null, method);
            }

            return s_sk3FilterDropShadowFactory(dropOffsetX, dropOffsetY, sigma, f, color);
        }
    }
}
