using System;
using SkiaSharp;

namespace Avalonia.Skia;

internal static partial class SkiaCompat
{
    private static readonly bool s_isSkiaSharp3 = typeof(SKPaint).Assembly.GetName().Version?.Major == 3;

#if !NET8_0_OR_GREATER
    private static Exception UnsupportedException()
    {
        return new InvalidOperationException("Avalonia doesn't support SkiaSharp 3.0 on .NET 7 and older. Please upgrade to .NET 8.");
    }
#endif
}
