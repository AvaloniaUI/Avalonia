using System;
using System.Runtime.InteropServices;
using SkiaSharp;

namespace Avalonia.Skia;

internal static unsafe partial class SkiaCompat
{
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

        static void NewCall(SKCanvas canvas, SKMatrix matrix)
        {
            var m44 = ToSkMatrix44(matrix);
            sk_canvas_set_matrix(canvas.Handle, &m44);
        }
        
        static void LegacyCall(SKCanvas canvas, SKMatrix matrix) =>
            canvas.SetMatrix(matrix);
    }

    [DllImport("libSkiaSharp", CallingConvention = CallingConvention.Cdecl)]
    internal static extern unsafe void sk_canvas_set_matrix(IntPtr ccanvas, sk_matrix44_t* cmatrix);

}
