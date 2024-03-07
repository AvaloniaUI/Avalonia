using System;
using System.Runtime.CompilerServices;
using SkiaSharp;

namespace Avalonia.Skia;

internal static unsafe partial class SkiaCompat
{
    static SkiaCompat()
    {
        var isSkiaSharp3 = typeof(SKPath).Assembly.GetName().Version?.Major == 3;
        s_canvasSetMatrix = GetCanvasSetMatrix(isSkiaSharp3);
        s_pathTransform = GetPathTransform(isSkiaSharp3);
        s_sk3FilterBlur = GetSKImageFilterCreateBlur(isSkiaSharp3);
        s_sk3FilterDropShadow = GetSKImageFilterCreateDropShadow(isSkiaSharp3);
    }

#if !NET8_0_OR_GREATER
    private static Exception UnsupportedException()
    {
        return new InvalidOperationException("Avalonia doesn't support SkiaSharp 3.0 on .NET 7 and older. Please upgrade to .NET 8.");
    }
#endif
}
