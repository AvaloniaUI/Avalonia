using Avalonia.Metal;
using CoreAnimation;

namespace Avalonia.iOS.Metal;

internal class MetalPlatformSurface : IMetalPlatformSurface
{
    private readonly CAMetalLayer _layer;
    private readonly AvaloniaView _avaloniaView;

    public MetalPlatformSurface(CAMetalLayer layer, AvaloniaView avaloniaView)
    {
        _layer = layer;
        _avaloniaView = avaloniaView;
    }
    public IMetalPlatformSurfaceRenderTarget CreateMetalRenderTarget(IMetalDevice device)
    {
        var dev = (MetalDevice)device;
        _layer.Device = dev.Device;

        var target = new MetalRenderTarget(_layer, dev);
        _avaloniaView.SetRenderTarget(target);
        return target;
    }
}
