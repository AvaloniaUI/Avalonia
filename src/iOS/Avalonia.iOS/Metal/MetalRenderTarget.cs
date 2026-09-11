using Avalonia.Media;
using Avalonia.Metal;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;
using CoreAnimation;
using CoreGraphics;

namespace Avalonia.iOS.Metal;

internal class MetalRenderTarget : IMetalPlatformSurfaceRenderTarget, IColorManagedRenderTarget
{
    private readonly CAMetalLayer _layer;
    private readonly MetalDevice _device;
    private (PixelSize size, double scaling) _lastLayout;

    // The layer holds an unretained reference, so it's kept alive for as long as the target lives.
    private readonly CGColorSpace? _colorSpace;

    public MetalRenderTarget(CAMetalLayer layer, MetalDevice device, PresentationColorSpace preferredColorSpace)
    {
        _layer = layer;
        _device = device;

        var resolvedColorSpace = Resolve(preferredColorSpace);

        // Left untagged when unavailable. 8 bit BGRA already covers the Display P3 gamut.
        var colorSpaceName = resolvedColorSpace switch
        {
            PresentationColorSpace.Srgb => CGColorSpaceNames.Srgb,
            PresentationColorSpace.DisplayP3 => CGColorSpaceNames.DisplayP3,
            _ => null
        };

        if (colorSpaceName is not null && CGColorSpace.CreateWithName(colorSpaceName) is { } colorSpace)
        {
            _colorSpace = colorSpace;
            _layer.ColorSpace = colorSpace;
            ColorSpace = resolvedColorSpace;
        }
    }

    public PresentationColorSpace ColorSpace { get; } = PresentationColorSpace.Unspecified;

    /// <summary>
    /// Turns a request into the concrete color space it would be presented as. Display P3 is the
    /// widest gamut a CAMetalLayer can present without an extended range pixel format.
    /// </summary>
    /// <remarks>
    /// This does not need a layer, so the surface can answer before the first frame was drawn.
    /// </remarks>
    internal static PresentationColorSpace Resolve(PresentationColorSpace colorSpace) =>
        colorSpace == PresentationColorSpace.WideGamut ? PresentationColorSpace.DisplayP3 : colorSpace;

    public (PixelSize size, double scaling) PendingLayout { get; set; } = (new PixelSize(1, 1), 1);
    public void Dispose()
    {
    }

    public IMetalPlatformSurfaceRenderingSession BeginRendering()
    {
        var (size, scaling) = PendingLayout;
        if (_lastLayout != (size, scaling))
        {
            _lastLayout = (size, scaling);
            _layer.DrawableSize = new CGSize(size.Width, size.Height);
        }

        var drawable = _layer.NextDrawable() ?? throw new PlatformGraphicsContextLostException();
        return new MetalDrawingSession(_device, drawable, size, scaling);
    }
}
