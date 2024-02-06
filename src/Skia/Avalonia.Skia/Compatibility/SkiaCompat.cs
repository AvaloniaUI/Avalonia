using SkiaSharp;

namespace Avalonia.Skia;

internal static partial class SkiaCompat
{
    public static bool IsSkiaSharp3 { get; } = typeof(SKPath).Assembly.GetName().Version?.Major == 3;
}
