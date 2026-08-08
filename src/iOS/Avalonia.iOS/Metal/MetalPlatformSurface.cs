using System;
using Avalonia.Media;
using Avalonia.Metal;
using Avalonia.Threading;
using CoreAnimation;

namespace Avalonia.iOS.Metal;

internal class MetalPlatformSurface : IMetalPlatformSurface
{
    private readonly CAMetalLayer _layer;
    private readonly AvaloniaView _avaloniaView;
    private readonly PresentationColorSpace _preferredColorSpace;

    public MetalPlatformSurface(CAMetalLayer layer, AvaloniaView avaloniaView,
        PresentationColorSpace preferredColorSpace)
    {
        _layer = layer;
        _avaloniaView = avaloniaView;
        _preferredColorSpace = preferredColorSpace;

        // Known before the first frame, and only corrected when tagging the layer really fails.
        CurrentColorSpace = MetalRenderTarget.Resolve(preferredColorSpace);
    }

    public PresentationColorSpace CurrentColorSpace { get; private set; }

    public event EventHandler? CurrentColorSpaceChanged;

    public IMetalPlatformSurfaceRenderTarget CreateMetalRenderTarget(IMetalDevice device)
    {
        var dev = (MetalDevice)device;
        _layer.Device = dev.Device;

        var target = new MetalRenderTarget(_layer, dev, _preferredColorSpace);
        _avaloniaView.SetRenderTarget(target);
        SetCurrentColorSpace(target.ColorSpace);
        return target;
    }

    private void SetCurrentColorSpace(PresentationColorSpace colorSpace)
    {
        if (CurrentColorSpace == colorSpace)
            return;

        CurrentColorSpace = colorSpace;

        // Render targets are created off the UI thread, but the event is for application code.
        Dispatcher.UIThread.Post(() => CurrentColorSpaceChanged?.Invoke(this, EventArgs.Empty));
    }
}
