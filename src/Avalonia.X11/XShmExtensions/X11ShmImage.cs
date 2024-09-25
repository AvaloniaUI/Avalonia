using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Platform;

namespace Avalonia.X11.XShmExtensions;

class X11ShmFramebufferContext
{
    public X11ShmFramebufferContext(X11Window x11Window, IntPtr display, IntPtr windowHandle, IntPtr renderHandle)
    {
        X11Window = x11Window;
        Display = display;
        WindowHandle = windowHandle;
        RenderHandle = renderHandle;
    }

    public X11Window X11Window { get; }

    public IntPtr Display { get; }

    public IntPtr WindowHandle { get; }

    public IntPtr RenderHandle { get; }
}

internal class X11ShmFramebufferSurface : IFramebufferPlatformSurface
{
    public X11ShmFramebufferSurface(X11Window x11Window, IntPtr display, IntPtr windowHandle, IntPtr renderHandle)
    {
        _context = new X11ShmFramebufferContext(x11Window, display, windowHandle, renderHandle);
    }

    private readonly X11ShmFramebufferContext _context;

    public IFramebufferRenderTarget CreateFramebufferRenderTarget()
    {
        return new X11ShmImageSwapchain(_context);
    }

    public void OnXShmCompletionEvent(ref XEvent ev)
    {
        
    }
}

internal class X11ShmImage
{
    /// <summary>
    /// Returns false if we haven't got a completion event since the last Present, can call ProcessPendingEvents here
    /// </summary>
    public bool IsReady { get; }
}

internal class X11ShmImageManager
{

}

//class DeferredDisplayEvents
//{

//}

internal class X11ShmImageSwapchain : IFramebufferRenderTarget
{
    public X11ShmImageSwapchain(X11ShmFramebufferContext context)
    {
        _context = context;
    }

    private readonly X11ShmFramebufferContext _context;

    private Queue<X11ShmImage> _availableQueue = new();
    private Queue<X11ShmImage> _presentationQueue = new();
    private PixelSize? _lastSize;

    public void Dispose()
    {

    }

    private void DrainPresentationQueue()
    {
        while (_presentationQueue.Count > 0 && _presentationQueue.Peek().IsReady)
        {
            _availableQueue.Enqueue(_presentationQueue.Dequeue());
        }
    }

    public ILockedFramebuffer Lock()
    {
        /*
         1) gets the current window geometry, if it doesn't match the lastSize disposes images and clears queues
         2) calls DrainPresentationQueue();
         3) if there is an image in _availableQueue - use this image
         4) else if there are no images in _availableQueue and presentationQueue.Count < swapchainSize - create new image
         5) else synchronously wait for the first image from _presentationQueue and use it
         6) return a framebuffer associated with chosen image
         */
        throw new NotImplementedException();
    }
}
