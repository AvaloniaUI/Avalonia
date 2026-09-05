using Avalonia.Media;
using Avalonia.Metadata;

namespace Avalonia.Platform.Surfaces;

/// <summary>
/// Implemented by an <see cref="IPlatformRenderSurfaceRenderTarget"/> whose native surface is tagged
/// with a color space, so that the renderer can draw using the same one.
/// </summary>
/// <remarks>
/// Render targets which don't implement this are presented without an explicit color space.
/// </remarks>
[Unstable, PrivateApi]
public interface IColorManagedRenderTarget
{
    /// <summary>
    /// Gets the color space which was really applied, that is not necessary the requested one.
    /// </summary>
    PresentationColorSpace ColorSpace { get; }
}
