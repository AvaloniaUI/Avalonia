using System;
using Avalonia.Controls.Platform.Surfaces;

namespace Avalonia.X11.XShmExtensions;

internal class X11ShmFramebufferSurface : IFramebufferPlatformSurface
{
    public X11ShmFramebufferSurface(X11Window x11Window, IntPtr display, IntPtr windowHandle, IntPtr renderHandle, IntPtr visual, int depth)
    {
        _context = new X11ShmFramebufferContext(x11Window, display, windowHandle, renderHandle, visual, depth);
    }

    private readonly X11ShmFramebufferContext _context;

    public IFramebufferRenderTarget CreateFramebufferRenderTarget()
    {
        return new X11ShmImageSwapchain(_context);
    }

    public unsafe void OnXShmCompletionEvent(XEvent @event)
    {
        var p = &@event;
        var xShmCompletionEvent = (XShmCompletionEvent*)p;
        UInt64 shmseg = xShmCompletionEvent->shmseg;
        _context.OnXShmCompletion(shmseg);
    }
}