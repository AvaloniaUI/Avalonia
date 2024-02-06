using System;
using System.Runtime.InteropServices;
using Avalonia.Compatibility;
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
            if (OperatingSystemEx.IsIOS() || OperatingSystemEx.IsTvOS())
                sk_canvas_set_matrix_ios(canvas.Handle, &m44);
            else
                sk_canvas_set_matrix(canvas.Handle, &m44);
        }

        static void LegacyCall(SKCanvas canvas, SKMatrix matrix) =>
            canvas.SetMatrix(matrix);
    }

    [DllImport("libSkiaSharp", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void sk_canvas_set_matrix(IntPtr ccanvas, sk_matrix44_t* cmatrix);

    [DllImport("@rpath/libSkiaSharp.framework/libSkiaSharp", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void sk_canvas_set_matrix_ios(IntPtr ccanvas, sk_matrix44_t* cmatrix);
}
