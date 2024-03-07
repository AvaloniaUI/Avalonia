using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using SkiaSharp;

namespace Avalonia.Skia;

internal static unsafe partial class SkiaCompat
{
    private static readonly delegate* managed<SKCanvas, in SKMatrix, void> s_canvasSetMatrix;
    public static void CSetMatrix(this SKCanvas canvas, in SKMatrix matrix) => s_canvasSetMatrix(canvas, matrix);

    private static delegate* managed<SKCanvas, in SKMatrix, void> GetCanvasSetMatrix(bool isSkiaSharp3)
    {
        if (isSkiaSharp3)
        {
#if NET8_0_OR_GREATER
            return &NewCanvasSetMatrix;
#else
            throw UnsupportedException();
#endif
        }
        else
        {
            return &LegacyCall;

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
