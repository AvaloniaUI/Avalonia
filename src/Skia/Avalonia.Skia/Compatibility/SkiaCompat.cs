using System;
using System.Runtime.CompilerServices;
using SkiaSharp;

namespace Avalonia.Skia;

internal static unsafe partial class SkiaCompat
{
    private static bool IsSkiaSharp3 { get; } = typeof(SKPath).Assembly.GetName().Version?.Major == 3;

    static SkiaCompat()
    {
        s_canvasSetMatrix = GetCanvasSetMatrix();
        s_pathTransform = GetPathTransform();
        s_sk3FilterBlur = GetSKImageFilterCreateBlur();
        s_sk3FilterDropShadow = GetSKImageFilterCreateDropShadow();
    }

#if !NET8_0_OR_GREATER
    private static Exception UnsupportedException()
    {
        return new InvalidOperationException("Avalonia doesn't support SkiaSharp 3.0 on .NET 7 and older. Please upgrade to .NET 8.");
    }
#endif
}
