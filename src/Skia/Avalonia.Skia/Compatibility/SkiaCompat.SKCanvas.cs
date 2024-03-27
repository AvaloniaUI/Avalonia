using System;
using System.Runtime.CompilerServices;
using SkiaSharp;

namespace Avalonia.Skia;

internal static partial class SkiaCompat
{
    public static void SetMatrix(SKCanvas canvas, in SKMatrix matrix)
    {
        if (s_isSkiaSharp3)
        {
#if NET8_0_OR_GREATER
            NewCanvasSetMatrix(canvas, matrix);
#else
            throw UnsupportedException();
#endif
        }
        else
        {
            LegacyCall(canvas, matrix);

            static void LegacyCall(SKCanvas canvas, in SKMatrix matrix)
            {
                canvas.SetMatrix(matrix);
            }
        }
    }

#if NET8_0_OR_GREATER
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "SetMatrix")]
    private static extern void NewCanvasSetMatrix(SKCanvas canvas, in SKMatrix matrix);
#endif
}
