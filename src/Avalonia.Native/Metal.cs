using System;
using Avalonia.Metal;
using Avalonia.Native.Interop;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.Utilities;

namespace Avalonia.Native;

class MetalPlatformGraphics : IPlatformGraphics
{
    private readonly IAvnMetalDisplay _display;

    public MetalPlatformGraphics(IAvaloniaNativeFactory factory)
    {
        _display = factory.ObtainMetalDisplay();
    }
    public bool UsesSharedContext => false;
    public IPlatformGraphicsContext CreateContext() => new MetalDevice(_display.CreateDevice());

    public IPlatformGraphicsContext GetSharedContext() => throw new NotSupportedException();
}

class MetalDevice : IMetalDevice
{
    public IAvnMetalDevice Native { get; private set; }
    private DisposableLock _syncRoot = new();
    

    public MetalDevice(IAvnMetalDevice native)
    {
        Native = native;
    }

    public void Dispose()
    {
        Native?.Dispose();
        Native = null;
    }

    public object TryGetFeature(Type featureType) => null;

    public bool IsLost => false;

    public IDisposable EnsureCurrent() => _syncRoot.Lock();

    public IntPtr Device => Native.Device;
    public IntPtr CommandQueue => Native.Queue;
}

class MetalPlatformSurface : IMetalPlatformSurface
{
    private readonly IAvnWindowBase _window;

    public MetalPlatformSurface(IAvnWindowBase window)
    {
        _window = window;
    }
    public IMetalPlatformSurfaceRenderTarget CreateMetalRenderTarget(IMetalDevice device)
    {
        if (!Dispatcher.UIThread.CheckAccess())
            throw new RenderTargetNotReadyException();
        
        var dev = (MetalDevice)device;
        var target = _window.CreateMetalRenderTarget(dev.Native);
        return new MetalRenderTarget(target);
    }
}

internal class MetalRenderTarget : IMetalPlatformSurfaceRenderTarget
{
    private IAvnMetalRenderTarget _native;

    public MetalRenderTarget(IAvnMetalRenderTarget native)
    {
        _native = native;
    }

    public void Dispose()
    {
        _native?.Dispose();
        _native = null;
    }

    public IMetalPlatformSurfaceRenderingSession BeginRendering()
    {
        var session = _native.BeginDrawing();
        return new MetalDrawingSession(session);
    }
}

internal class MetalDrawingSession : IMetalPlatformSurfaceRenderingSession
{
    private IAvnMetalRenderingSession _session;

    public MetalDrawingSession(IAvnMetalRenderingSession session)
    {
        _session = session;
    }

    public void Dispose()
    {
        _session?.Dispose();
        _session = null;
    }

    public IntPtr Texture => _session.Texture;
    public PixelSize Size
    {
        get
        {
            var size = _session.PixelSize;
            return new(size.Width, size.Height);
        }
    }

    public double Scaling => _session.Scaling;
    public bool IsYFlipped => false;
}
