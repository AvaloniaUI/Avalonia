using System;
using Avalonia.Metal;
using CoreAnimation;

namespace Avalonia.iOS.Metal;

internal class MetalDrawingSession : IMetalPlatformSurfaceRenderingSession
{
    private readonly MetalDevice _device;
    private readonly ICAMetalDrawable _drawable;

    public MetalDrawingSession(MetalDevice device, ICAMetalDrawable drawable, PixelSize size, double scaling)
    {
        _device = device;
        _drawable = drawable;
        Size = size;
        Scaling = scaling;
        Texture = _drawable.Texture.Handle;
    }

    public void Dispose()
    {
        var buffer = _device.Queue.CommandBuffer();
        buffer!.PresentDrawable(_drawable);
        buffer.Commit();
    }

    public IntPtr Texture { get; }
    public PixelSize Size { get; }

    public double Scaling { get; }

    public bool IsYFlipped => false;
}
