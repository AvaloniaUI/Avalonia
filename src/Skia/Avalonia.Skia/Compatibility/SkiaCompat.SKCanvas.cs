using System;
using System.Diagnostics.CodeAnalysis;
using SkiaSharp;

namespace Avalonia.Skia;

internal static partial class SkiaCompat
{
    private delegate void CanvasSetMatrixDelegate(SKCanvas canvas, in SKMatrix matrix);
    private static CanvasSetMatrixDelegate? s_canvasSetMatrix; 

    public static void CSetMatrix(this SKCanvas canvas, SKMatrix matrix)
    {
        if (IsSkiaSharp3)
        {
            NewCall(canvas, matrix);
        }
        else
        {
            LegacyCall(canvas, matrix);
        }

        [DynamicDependency("SetMatrix", typeof(SKCanvas))]
        static void NewCall(SKCanvas canvas, SKMatrix matrix)
        {
            if (s_canvasSetMatrix is null)
            {
                var method = typeof(SKCanvas).GetMethod("SetMatrix", new[] { typeof(SKMatrix).MakeByRefType() })!;
                s_canvasSetMatrix = (CanvasSetMatrixDelegate)Delegate.CreateDelegate(typeof(CanvasSetMatrixDelegate), method);
            }

            s_canvasSetMatrix(canvas, matrix);
        }

        static void LegacyCall(SKCanvas canvas, SKMatrix matrix) =>
            canvas.SetMatrix(matrix);
    }
}
