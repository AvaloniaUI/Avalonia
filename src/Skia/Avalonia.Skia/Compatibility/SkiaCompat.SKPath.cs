using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using SkiaSharp;

namespace Avalonia.Skia;

internal static unsafe partial class SkiaCompat
{
    private static readonly delegate* managed<SKPath, in SKMatrix, void> s_pathTransform;

    public static void CTransform(this SKPath path, in SKMatrix matrix) => s_pathTransform(path, matrix);

#if !NET8_0_OR_GREATER
    [DynamicDependency("Transform(SkiaSharp.SKMatrix)", typeof(SKPath))]
#endif
    private static delegate* managed<SKPath, in SKMatrix, void> GetPathTransform()
    {
        if (IsSkiaSharp3)
        {
#if NET8_0_OR_GREATER
            return &NewPathTransform;
#else
            var method = typeof(SKPath).GetMethod("Transform", new[] { typeof(SKMatrix).MakeByRefType() })!;
            return (delegate* managed<SKPath, in SKMatrix, void>)method.MethodHandle.GetFunctionPointer();
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
