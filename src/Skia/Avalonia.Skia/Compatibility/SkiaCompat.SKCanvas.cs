using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using SkiaSharp;

namespace Avalonia.Skia;

internal static unsafe partial class SkiaCompat
{
    private static readonly delegate* managed<SKCanvas, in SKMatrix, void> s_canvasSetMatrix;
    public static void CSetMatrix(this SKCanvas canvas, in SKMatrix matrix) => s_canvasSetMatrix(canvas, matrix);

#if !NET8_0_OR_GREATER
    [DynamicDependency("SetMatrix(SkiaSharp.SKMatrix)", typeof(SKCanvas))]
#endif
    private static delegate* managed<SKCanvas, in SKMatrix, void> GetCanvasSetMatrix()
    {
        if (IsSkiaSharp3)
        {
#if NET8_0_OR_GREATER
            return &NewCanvasSetMatrix;
#else
            var method = typeof(SKCanvas).GetMethod("SetMatrix", new[] { typeof(SKMatrix).MakeByRefType() })!;
            return (delegate* managed<SKCanvas, in SKMatrix, void>)method.MethodHandle.GetFunctionPointer();
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
