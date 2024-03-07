using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using SkiaSharp;

namespace Avalonia.Skia;

internal static unsafe partial class SkiaCompat
{
    private static readonly delegate* managed<float, float, SKImageFilter> s_sk3FilterBlur;
    private static readonly delegate* managed<float, float, float, float, SKColor, SKImageFilter> s_sk3FilterDropShadow;

    public static SKImageFilter CreateBlur(float sigmaX, float sigmaY) => s_sk3FilterBlur(sigmaX, sigmaY);

    public static SKImageFilter CreateDropShadow(float dropOffsetX, float dropOffsetY, float sigma, float f, SKColor color)
        => s_sk3FilterDropShadow(dropOffsetX, dropOffsetY, sigma, f, color);

    private static delegate* managed<float, float, SKImageFilter> GetSKImageFilterCreateBlur()
    {
        if (IsSkiaSharp3)
        {
#if NET8_0_OR_GREATER
            return &NewSKImageFilterCreateBlur;
#else
            throw UnsupportedException();
#endif
        }
        else
        {
            return &LegacyBlurCall;

            static SKImageFilter LegacyBlurCall(float sigmaX, float sigmaY) =>
                SKImageFilter.CreateBlur(sigmaX, sigmaY);
        }
    }

    private static delegate* managed<float, float, float, float, SKColor, SKImageFilter> GetSKImageFilterCreateDropShadow()
    {
        if (IsSkiaSharp3)
        {
#if NET8_0_OR_GREATER
            return &NewSKImageFilterCreateDropShadow;
#else
            throw UnsupportedException();
#endif
        }
        else
        {
            return &LegacyDropShadowCall;

            static SKImageFilter LegacyDropShadowCall(float dropOffsetX, float dropOffsetY, float sigma, float f, SKColor color) =>
                SKImageFilter.CreateDropShadow(dropOffsetX, dropOffsetY, sigma, f, color);
        }
    }

#if NET8_0_OR_GREATER
    // See https://github.com/dotnet/runtime/issues/90081 why we need `SKImageFilter _`
    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "CreateBlur")]
    private static extern SKImageFilter NewSKImageFilterCreateBlur(SKImageFilter? _, float sigmaX, float sigmaY);
    private static SKImageFilter NewSKImageFilterCreateBlur(float sigmaX, float sigmaY)
        => NewSKImageFilterCreateBlur(null, sigmaX, sigmaY);

    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "CreateDropShadow")]
    private static extern SKImageFilter NewSKImageFilterCreateDropShadow(SKImageFilter? _, float dropOffsetX,
        float dropOffsetY, float sigma, float f, SKColor color);
    private static SKImageFilter NewSKImageFilterCreateDropShadow(float dropOffsetX, float dropOffsetY, float sigma, float f, SKColor color)
        => NewSKImageFilterCreateDropShadow(null, dropOffsetX, dropOffsetY, sigma, f, color);
#endif
}
