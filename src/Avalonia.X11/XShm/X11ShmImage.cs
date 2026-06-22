using System;
using System.Diagnostics;
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
        PShmSegmentInfo = pShmSegmentInfo;

        const int ZPixmap = 2;

        var width = size.Width;
        var height = size.Height;

        Size = size;

        Debug.Assert(depth is 32, "The PixelFormat must be Bgra8888, so that the depth should be 32.");

        IntPtr data = IntPtr.Zero;

        var shmImage = (XImage*)XShmCreateImage(deferredDisplay, visual, (uint)depth, ZPixmap, data, pShmSegmentInfo,
            (uint)width, (uint)height);
        ShmImage = shmImage;

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

    private XImage* ShmImage { get; }
    private XShmSegmentInfo* PShmSegmentInfo { get; }
    public IntPtr ShmAddr => PShmSegmentInfo->shmaddr;

    public const int ByteSizeOfPixel = 4;

    public PixelSize Size { get; }

    public UIntPtr ShmSeg => PShmSegmentInfo->shmseg;

    /// <summary>
    /// Submits this image to the given window via XShmPutImage and requests a completion event. Returns
    /// false if the server rejected the request, in which case no completion event will be delivered.
    /// </summary>
    public bool Put(IntPtr windowXId)
    {
        // The DeferredDisplay connection is owned by the rendering thread, so no XLockDisplay is needed -
        // XInitThreads already serializes individual Xlib calls.
        var gc = XCreateGC(_deferredDisplay, windowXId, 0, IntPtr.Zero);
        var status = XShmPutImage(_deferredDisplay, windowXId, gc, ShmImage, 0, 0, 0, 0, (uint)Size.Width,
            (uint)Size.Height, send_event: true);
        XFlush(_deferredDisplay);
        XFreeGC(_deferredDisplay, gc);
        return status != 0;
    }

    public void Dispose()
    {
        XShmDetach(_deferredDisplay, PShmSegmentInfo);

        shmdt(ShmAddr);
        shmctl(PShmSegmentInfo->shmid, IPC_RMID, IntPtr.Zero);

        Marshal.FreeHGlobal(new IntPtr(PShmSegmentInfo));

        Logger.TryGet(LogEventLevel.Debug, LogArea.X11Platform)?.Log(this, "[X11ShmImage] Dispose");
    }
}
