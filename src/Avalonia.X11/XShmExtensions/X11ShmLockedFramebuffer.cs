using System;
using Avalonia.Platform;

namespace Avalonia.X11.XShmExtensions;

class X11ShmLockedFramebuffer : ILockedFramebuffer
{
    public X11ShmLockedFramebuffer(X11ShmImage shmImage, X11ShmFramebufferContext context)
    {
        _context = context;
        X11ShmImage = shmImage;
    }

    public void Dispose()
    {
        SendRender();
    }

    private readonly X11ShmFramebufferContext _context;

    public IntPtr Address => X11ShmImage.ShmAddr;
    public PixelSize Size => X11ShmImage.Size;
    public int RowBytes => X11ShmImage.Size.Width * X11ShmImage.ByteSizeOfPixel;
    public Vector Dpi => new Vector(96, 96);
    public PixelFormat Format => PixelFormat.Bgra8888;
    public X11ShmImage X11ShmImage { get; }

    private unsafe void SendRender()
    {
        // Send XShmImage and register it to handle the XShmCompletionEvent
        _context.RegisterX11ShmImage(X11ShmImage);
        var display = _context.Display;
        var xid = _context.RenderHandle;
        var gc = XLib.XCreateGC(display, xid, 0, IntPtr.Zero);
        var exposeX = 0;
        var exposeY = 0;
        var exposeWidth = Size.Width;
        var exposeHeight = Size.Height;

        XShm.XShmPutImage(display, xid, gc, X11ShmImage.ShmImage, exposeX, exposeY, exposeX, exposeY, (uint)exposeWidth, (uint)exposeHeight,
            send_event: true);

        XLib.XFreeGC(display, gc);

        X11ShmDebugLogger.WriteLine($"[X11ShmLockedFramebuffer] SendRender");
    }
}
