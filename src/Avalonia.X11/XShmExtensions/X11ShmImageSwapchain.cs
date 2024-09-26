using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Platform;

namespace Avalonia.X11.XShmExtensions;

internal class X11ShmImageSwapchain : IFramebufferRenderTarget
{
    public X11ShmImageSwapchain(X11ShmFramebufferContext context)
    {
        _context = context;
        X11ShmImageManager = new X11ShmImageManager(context);
    }

    public X11ShmImageManager X11ShmImageManager { get; }

    private readonly X11ShmFramebufferContext _context;

    public void Dispose()
    {
        X11ShmImageManager.Dispose();
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
        var display = _context.Display;
        var xid = _context.WindowXId;
        XLib.XLockDisplay(display);
        XLib.XGetGeometry(display, xid, out var root, out var x, out var y, out var width, out var height,
            out var bw, out var d);
        XLib.XUnlockDisplay(display);

        var size = new PixelSize(width, height);
        var shmImage = X11ShmImageManager.GetOrCreateImage(size);

        return new X11ShmLockedFramebuffer(shmImage, _context);
    }
}