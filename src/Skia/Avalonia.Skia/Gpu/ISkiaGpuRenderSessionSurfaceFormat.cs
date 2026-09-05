using SkiaSharp;

namespace Avalonia.Skia;

/// <summary>
/// The pixel format a render session's surface actually uses, for the intermediate surfaces that
/// are blitted into it. A blit copies raw pixels and converts nothing, so an intermediate that does
/// not match presents the wrong values: an 8 bit one in front of a half float scRGB target hands
/// the compositor sRGB encoded values to interpret as linear light.
/// </summary>
internal interface ISkiaGpuRenderSessionSurfaceFormat
{
    SKColorType ColorType { get; }
    SKColorSpace? ColorSpace { get; }
}
