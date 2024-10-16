using System;
using Avalonia.Controls.Platform.Surfaces;

using ShmSeg = System.UInt64;

namespace Avalonia.X11.XShmExtensions;

internal class X11ShmFramebufferSurface : IFramebufferPlatformSurface
{
    public X11ShmFramebufferSurface(X11Window x11Window, IntPtr display, IntPtr windowHandle, IntPtr visual, int depth,
        bool shouldRenderOnUiThread)
    {
        // From https://www.x.org/releases/X11R7.5/doc/Xext/mit-shm.html
        // > The event type value that will be used can be determined at run time with a line of the form:
        // > int CompletionType = XShmGetEventBase (display) + ShmCompletion;
        const int ShmCompletion = 0;
        XShmCompletionType = XShm.XShmGetEventBase(display) + ShmCompletion;

        _context = new X11ShmFramebufferContext(x11Window, display, windowHandle, visual, depth, shouldRenderOnUiThread);
    }

    public int XShmCompletionType { get; }

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
