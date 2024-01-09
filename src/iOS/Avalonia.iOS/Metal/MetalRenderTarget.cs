using Avalonia.Metal;
using Avalonia.Platform;
using CoreAnimation;
using CoreGraphics;
using Foundation;

namespace Avalonia.iOS;

internal class MetalRenderTarget : IMetalPlatformSurfaceRenderTarget
{
    private readonly CAMetalLayer _layer;
    private readonly MetalDevice _device;
    private double _scaling = 1;
    private PixelSize _size = new(1, 1);

    public MetalRenderTarget(CAMetalLayer layer, MetalDevice device)
    {
        _layer = layer;
        _device = device;
    }

    public double PendingScaling { get; set; } = 1;
    public PixelSize PendingSize { get; set; } = new(1, 1);
    public void Dispose()
    {
    }

    public IMetalPlatformSurfaceRenderingSession BeginRendering()
    {
        // Flush all existing rendering
        var buffer = _device.Queue.CommandBuffer();
        buffer.Commit();
        buffer.WaitUntilCompleted();
        _size = PendingSize;
        _scaling= PendingScaling;
        _layer.DrawableSize = new CGSize(_size.Width, _size.Height);

        var drawable = _layer.NextDrawable() ?? throw new PlatformGraphicsContextLostException();
        return new MetalDrawingSession(_device, drawable, _size, _scaling);
    }
}
