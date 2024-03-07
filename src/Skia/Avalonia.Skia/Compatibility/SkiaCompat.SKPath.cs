using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using SkiaSharp;

namespace Avalonia.Skia;

internal static unsafe partial class SkiaCompat
{
    private static readonly delegate* managed<SKPath, in SKMatrix, void> s_pathTransform;

    public static void CTransform(this SKPath path, in SKMatrix matrix) => s_pathTransform(path, matrix);

    private static delegate* managed<SKPath, in SKMatrix, void> GetPathTransform(bool isSkiaSharp3)
    {
        if (isSkiaSharp3)
        {
#if NET8_0_OR_GREATER
            return &NewPathTransform;
#else
            throw UnsupportedException();
#endif
        }
        else
        {
            return &LegacyCall;

            static void LegacyCall(SKPath path, in SKMatrix matrix) =>
                path.Transform(matrix);
        }
    }

#if NET8_0_OR_GREATER
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "Transform")]
    private static extern void NewPathTransform(SKPath path, in SKMatrix matrix);
#endif
}
