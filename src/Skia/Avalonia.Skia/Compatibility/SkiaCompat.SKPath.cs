using System;
using System.Runtime.InteropServices;
using SkiaSharp;

namespace Avalonia.Skia;

internal static unsafe partial class SkiaCompat
{
    public static void CTransform(this SKPath path, ref SKMatrix matrix)
    {
        fixed (SKMatrix* m = &matrix)
            sk_path_transform(path.Handle, m);
    }

    [DllImport("libSkiaSharp", CallingConvention = CallingConvention.Cdecl)]
    private static extern void sk_path_transform(IntPtr cpath, SKMatrix* cmatrix);
}
