using Avalonia.Metal;
using Avalonia.Platform;
using CoreAnimation;
using CoreGraphics;

namespace Avalonia.iOS.Metal;

internal class MetalRenderTarget : IMetalPlatformSurfaceRenderTarget
{
    private readonly CAMetalLayer _layer;
    private readonly MetalDevice _device;
    private (PixelSize size, double scaling) _lastLayout;

    public MetalRenderTarget(CAMetalLayer layer, MetalDevice device)
    {
        _layer = layer;
        _device = device;
    }

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
