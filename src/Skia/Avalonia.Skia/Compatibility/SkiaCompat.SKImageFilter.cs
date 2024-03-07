using System;
using System.Runtime.CompilerServices;
using SkiaSharp;

namespace Avalonia.Skia;

internal static partial class SkiaCompat
{
    public static SKImageFilter CreateBlur(float sigmaX, float sigmaY)
    {
        if (s_isSkiaSharp3)
        {
#if NET8_0_OR_GREATER
            return NewSKImageFilterCreateBlur(null, sigmaX, sigmaY);
#else
            throw UnsupportedException();
#endif
        }
        else
        {
            return LegacyBlurCall(sigmaX, sigmaY);

            static SKImageFilter LegacyBlurCall(float sigmaX, float sigmaY) =>
                SKImageFilter.CreateBlur(sigmaX, sigmaY);
        }
    }

    public static SKImageFilter CreateDropShadow(float dropOffsetX, float dropOffsetY, float sigma, float f,
        SKColor color)
    {
        if (s_isSkiaSharp3)
        {
#if NET8_0_OR_GREATER
            return NewSKImageFilterCreateDropShadow(null!, dropOffsetX, dropOffsetY, sigma, f, color);
#else
            throw UnsupportedException();
#endif
        }
        else
        {
            return LegacyDropShadowCall(dropOffsetX, dropOffsetY, sigma, f, color);

            static SKImageFilter LegacyDropShadowCall(float dropOffsetX, float dropOffsetY, float sigma, float f, SKColor color) =>
                SKImageFilter.CreateDropShadow(dropOffsetX, dropOffsetY, sigma, f, color);
        }
    }

#if NET8_0_OR_GREATER
    // See https://github.com/dotnet/runtime/issues/90081 why we need `SKImageFilter _`
    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "CreateBlur")]
    private static extern SKImageFilter NewSKImageFilterCreateBlur(SKImageFilter? _, float sigmaX, float sigmaY);

    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "CreateDropShadow")]
    private static extern SKImageFilter NewSKImageFilterCreateDropShadow(SKImageFilter? _, float dropOffsetX,
        float dropOffsetY, float sigma, float f, SKColor color);
#endif
}
