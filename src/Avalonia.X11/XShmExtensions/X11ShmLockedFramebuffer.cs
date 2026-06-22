using System;
using Avalonia.Logging;
using Avalonia.Platform;

namespace Avalonia.X11.XShmExtensions;

class X11ShmLockedFramebuffer : ILockedFramebuffer
{
    public X11ShmLockedFramebuffer(X11ShmImageSwapchain owner, X11ShmImage shmImage)
    {
        _owner = owner;
        X11ShmImage = shmImage;
    }

    public void Dispose()
    {
        SendRender();
    }

    private readonly X11ShmImageSwapchain _owner;

    public IntPtr Address => X11ShmImage.ShmAddr;
    public PixelSize Size => X11ShmImage.Size;
    public int RowBytes => X11ShmImage.Size.Width * X11ShmImage.ByteSizeOfPixel;
    public Vector Dpi => new Vector(96, 96);
    public PixelFormat Format => PixelFormat.Bgra8888;
    public AlphaFormat AlphaFormat => AlphaFormat.Premul;
    public X11ShmImage X11ShmImage { get; }

    private unsafe void SendRender()
    {
        var display = _owner.DeferredDisplay;
        var xid = _owner.WindowXId;

        var gc = XLib.XCreateGC(display, xid, 0, IntPtr.Zero);

        // The DeferredDisplay connection is owned by the rendering thread, so no XLockDisplay is needed -
        // XInitThreads already serializes individual Xlib calls.
        var status = XShm.XShmPutImage(display, xid, gc, X11ShmImage.ShmImage, 0, 0, 0, 0, (uint)Size.Width,
            (uint)Size.Height, send_event: true);
        XLib.XFlush(display);
        XLib.XFreeGC(display, gc);

        if (status != 0)
        {
            // The request was accepted, so the server will emit a matching XShmCompletionEvent. Register the
            // image for completion routing and count it as in flight only now, so a failed put can never leave
            // the backpressure counter stuck (a frame that never completes would otherwise block Lock forever).
            _owner.Dispatcher.RegisterInFlight(X11ShmImage);
            _owner.OnImageSubmitted(X11ShmImage);
            Logger.TryGet(LogEventLevel.Debug, LogArea.X11Platform)?.Log(this, "[X11ShmLockedFramebuffer] SendRender XShmPutImage");
        }
        else
        {
            // No completion will arrive for a failed put; drop the image so it never counts toward backpressure.
            Logger.TryGet(LogEventLevel.Warning, LogArea.X11Platform)?.Log(this, "[X11ShmLockedFramebuffer] XShmPutImage failed, dropping image");
            _owner.OnImageDropped(X11ShmImage);
        }
    }
}
