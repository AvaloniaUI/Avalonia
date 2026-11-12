using System;
using Avalonia.Logging;
using Avalonia.Platform.Surfaces;

namespace Avalonia.X11.XShm;

internal class X11ShmFramebufferSurface : IFramebufferPlatformSurface
{
    public X11ShmFramebufferSurface(IntPtr deferredDisplay, IntPtr windowHandle, IntPtr visual, int depth,
        X11DeferredDisplayDispatcher dispatcher)
    {
        _deferredDisplay = deferredDisplay;
        _windowHandle = windowHandle;
        _visual = visual;
        _depth = depth;
        _dispatcher = dispatcher;
    }

    private readonly IntPtr _deferredDisplay;
    private readonly IntPtr _windowHandle;
    private readonly IntPtr _visual;
    private readonly int _depth;
    private readonly X11DeferredDisplayDispatcher _dispatcher;

    public IFramebufferRenderTarget CreateFramebufferRenderTarget()
    {
        Logger.TryGet(LogEventLevel.Debug, LogArea.X11Platform)?.Log(this, "[X11ShmFramebufferSurface] CreateFramebufferRenderTarget");

        return new X11ShmFramebufferRenderTarget(_deferredDisplay, _windowHandle, _visual, _depth, _dispatcher);
    }
}
