using System;
using Avalonia.Controls.Platform.Surfaces;

using ShmSeg = System.UInt64;

namespace Avalonia.X11.XShmExtensions;

internal class X11ShmFramebufferSurface : IFramebufferPlatformSurface
{
    public X11ShmFramebufferSurface(X11Window x11Window, IntPtr display, IntPtr windowHandle, IntPtr visual, int depth)
    {
        _context = new X11ShmFramebufferContext(x11Window, display, windowHandle, visual, depth);
    }

    private readonly X11ShmFramebufferContext _context;

    public IFramebufferRenderTarget CreateFramebufferRenderTarget()
    {
        X11ShmDebugLogger.WriteLine("[X11ShmFramebufferSurface] CreateFramebufferRenderTarget");

        return new X11ShmImageSwapchain(_context);
    }

    public unsafe void OnXShmCompletionEvent(XEvent @event)
    {
        var p = &@event;
        var xShmCompletionEvent = (XShmCompletionEvent*)p;
        ShmSeg shmseg = xShmCompletionEvent->shmseg;
        X11ShmDebugLogger.WriteLine($"[X11ShmFramebufferSurface][OnXShmCompletionEvent] shmseg={shmseg}");
        _context.OnXShmCompletion(shmseg);
    }
}
