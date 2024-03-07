using System.Runtime.CompilerServices;
using SkiaSharp;

namespace Avalonia.Skia;

internal static unsafe partial class SkiaCompat
{
    private static bool IsSkiaSharp3 { get; } = typeof(SKPath).Assembly.GetName().Version?.Major == 3;

    static SkiaCompat()
    {
        s_canvasSetMatrix = GetCanvasSetMatrix();
        s_pathTransform = GetPathTransform();
        s_sk3FilterBlur = GetSKImageFilterCreateBlur();
        s_sk3FilterDropShadow = GetSKImageFilterCreateDropShadow();
    }
}
