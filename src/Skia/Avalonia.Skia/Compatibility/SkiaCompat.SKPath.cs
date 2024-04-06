using System;
using System.Runtime.CompilerServices;
using SkiaSharp;

namespace Avalonia.Skia;

internal static partial class SkiaCompat
{
    // TODO: remove as https://github.com/mono/SkiaSharp/pull/2789 is shipped.
    public static void Transform(SKPath path, in SKMatrix matrix)
    {
        if (s_isSkiaSharp3)
        {
#if NET8_0_OR_GREATER
            NewPathTransform(path, matrix);
#else
            throw UnsupportedException();
#endif
        }
        else
        {
            LegacyCall(path, matrix);

            static void LegacyCall(SKPath path, in SKMatrix matrix) =>
                path.Transform(matrix);
        }
    }

#if NET8_0_OR_GREATER
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "Transform")]
    private static extern void NewPathTransform(SKPath path, in SKMatrix matrix);
#endif
}
