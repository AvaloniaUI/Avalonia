using System;
using System.Diagnostics;
using System.Threading;
using Avalonia.Logging;
using System.Runtime.InteropServices;
using static Avalonia.X11.LibC;
using static Avalonia.X11.XLib;

namespace Avalonia.X11.XShm;

internal unsafe class X11ShmImage : IDisposable
{
    public X11ShmImage(PixelSize size, IntPtr deferredDisplay, IntPtr visual, int depth)
    {
        _deferredDisplay = deferredDisplay;
        // The XShmSegmentInfo struct will store in XImage, and it must pin the address.
        IntPtr pXShmSegmentInfo = Marshal.AllocHGlobal(Marshal.SizeOf<XShmSegmentInfo>());
        var pShmSegmentInfo = (XShmSegmentInfo*)pXShmSegmentInfo;
        _shmSegmentInfo = pXShmSegmentInfo;

        const int ZPixmap = 2;

        var width = size.Width;
        var height = size.Height;

        Size = size;

        Debug.Assert(depth is 32, "The PixelFormat must be Bgra8888, so that the depth should be 32.");

        IntPtr data = IntPtr.Zero;

        var shmImage = (XImage*)XShmCreateImage(deferredDisplay, visual, (uint)depth, ZPixmap, data, pShmSegmentInfo,
            (uint)width, (uint)height);
        _shmImage = (IntPtr)shmImage;

        var mapLength = new IntPtr(width * ByteSizeOfPixel * height);
        var shmid = shmget(IPC_PRIVATE, mapLength, IPC_CREAT | 0777);
        pShmSegmentInfo->shmid = shmid;

        var shmaddr = shmat(shmid, IntPtr.Zero, 0);

        if(shmaddr == new IntPtr(-1))
        {
            shmctl(shmid, IPC_RMID, IntPtr.Zero);
            throw new InvalidOperationException("Failed to shmat");
        }

        pShmSegmentInfo->shmaddr = shmaddr;
        shmImage->data = data = shmaddr;

        XShmAttach(deferredDisplay, pShmSegmentInfo);

        Logger.TryGet(LogEventLevel.Debug, LogArea.X11Platform)?.Log(this, "[X11ShmImage] CreateX11ShmImage Size={Size} shmid={Shmid:X} shmaddr={ShmAddr}", Size, shmid, shmaddr);
    }

    private readonly IntPtr _deferredDisplay;
    private IntPtr _shmImage; // XImage*
    private IntPtr _shmSegmentInfo; // XShmSegmentInfo*

    public IntPtr ShmAddr => ((XShmSegmentInfo*)_shmSegmentInfo)->shmaddr;

    public const int ByteSizeOfPixel = 4;

    public PixelSize Size { get; }

    public UIntPtr ShmSeg => ((XShmSegmentInfo*)_shmSegmentInfo)->shmseg;

    /// <summary>
    /// Submits this image to the given window via XShmPutImage and requests a completion event. Returns
    /// false if the server rejected the request, in which case no completion event will be delivered.
    /// </summary>
    public bool Put(IntPtr windowXId)
    {
        // The DeferredDisplay connection is owned by the rendering thread, so no XLockDisplay is needed -
        // XInitThreads already serializes individual Xlib calls.
        var gc = XCreateGC(_deferredDisplay, windowXId, 0, IntPtr.Zero);
        var status = XShmPutImage(_deferredDisplay, windowXId, gc, (XImage*)_shmImage, 0, 0, 0, 0, (uint)Size.Width,
            (uint)Size.Height, send_event: true);
        XFlush(_deferredDisplay);
        XFreeGC(_deferredDisplay, gc);
        return status != 0;
    }

    public void Dispose()
    {
        // Atomically claim the image pointer; whoever swaps out a non-zero value owns the teardown, so a
        // double dispose is a no-op and never double-frees.
        var pShmImage = (XImage*)Interlocked.Exchange(ref _shmImage, IntPtr.Zero);
        if (pShmImage == null)
            return;

        var pShmSegmentInfo = (XShmSegmentInfo*)_shmSegmentInfo;
        _shmSegmentInfo = IntPtr.Zero;

        // Teardown order per the MIT-SHM spec: detach the server, destroy the image, then drop the segment.
        // https://xorg.freedesktop.org/archive/X11R7.7/doc/xextproto/shm.html
        XShmDetach(_deferredDisplay, pShmSegmentInfo);
        // Clear data first so XDestroyImage frees only the XImage structure - the shm segment is ours to drop
        // below; otherwise XDestroyImage would call free() on the shmat() pointer.
        pShmImage->data = IntPtr.Zero;
        XDestroyImage(ref *pShmImage);
        shmdt(pShmSegmentInfo->shmaddr);
        shmctl(pShmSegmentInfo->shmid, IPC_RMID, IntPtr.Zero);

        Marshal.FreeHGlobal((IntPtr)pShmSegmentInfo);

        Logger.TryGet(LogEventLevel.Debug, LogArea.X11Platform)?.Log(this, "[X11ShmImage] Dispose");
    }
}
