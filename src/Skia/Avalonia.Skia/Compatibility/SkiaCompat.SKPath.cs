using System;
using System.Diagnostics.CodeAnalysis;
using SkiaSharp;

namespace Avalonia.Skia;

internal static partial class SkiaCompat
{
    private delegate void PathTransformDelegate(SKPath canvas, in SKMatrix matrix);
    private static PathTransformDelegate? s_pathTransform; 

    public static void CTransform(this SKPath path, ref SKMatrix matrix)
    {
        if (IsSkiaSharp3)
        {
            NewCall(path, matrix);
        }
        else
        {
            LegacyCall(path, matrix);
        }

        [DynamicDependency("Transform", typeof(SKPath))]
        static void NewCall(SKPath path, SKMatrix matrix)
        {
            if (s_pathTransform is null)
            {
                var method = typeof(SKPath).GetMethod("Transform", new[] { typeof(SKMatrix).MakeByRefType() })!;
                s_pathTransform = (PathTransformDelegate)Delegate.CreateDelegate(typeof(PathTransformDelegate), method);
            }

            s_pathTransform(path, matrix);
        }

        static void LegacyCall(SKPath path, SKMatrix matrix) =>
            path.Transform(matrix);
    }
}
