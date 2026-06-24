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
    private readonly IntPtr _deferredDisplay;
    private XImage* _shmImage;
    private XShmSegmentInfo* _shmSegmentInfo;
    private int _disposed;
    
    public X11ShmImage(PixelSize size, IntPtr deferredDisplay, IntPtr visual, int depth)
    {
        _deferredDisplay = deferredDisplay;
        // The XShmSegmentInfo struct will be stored in XImage, and it must pin the address.
        _shmSegmentInfo = (XShmSegmentInfo*)Marshal.AllocHGlobal(Marshal.SizeOf<XShmSegmentInfo>());
        
        const int ZPixmap = 2;

        var width = size.Width;
        var height = size.Height;

        Size = size;

        Debug.Assert(depth is 32, "The PixelFormat must be Bgra8888, so that the depth should be 32.");

        _shmImage = XShmCreateImage(deferredDisplay, visual, (uint)depth, ZPixmap, IntPtr.Zero, _shmSegmentInfo,
            (uint)width, (uint)height);

        var mapLength = new IntPtr(width * ByteSizeOfPixel * height);
        var shmid = shmget(IPC_PRIVATE, mapLength, IPC_CREAT | 0777);
        _shmSegmentInfo->shmid = shmid;

        var shmaddr = shmat(shmid, IntPtr.Zero, 0);

        if(shmaddr == new IntPtr(-1))
        {
            shmctl(shmid, IPC_RMID, IntPtr.Zero);
            XDestroyImage(_shmImage);
            throw new InvalidOperationException("Failed to shmat");
        }

        _shmSegmentInfo->shmaddr = shmaddr;
        _shmImage->data = shmaddr;

        XShmAttach(deferredDisplay, _shmSegmentInfo);

        Logger.TryGet(LogEventLevel.Debug, LogArea.X11Platform)?.Log(this, "[X11ShmImage] CreateX11ShmImage Size={Size} shmid={Shmid:X} shmaddr={ShmAddr}", Size, shmid, shmaddr);
    }
    

    public IntPtr ShmAddr => _shmSegmentInfo->shmaddr;

    public const int ByteSizeOfPixel = 4;

    public PixelSize Size { get; }

    public UIntPtr ShmSeg => _shmSegmentInfo->shmseg;

    /// <summary>
    /// Submits this image to the given window via XShmPutImage and requests a completion event. Returns
    /// false if the server rejected the request, in which case no completion event will be delivered.
    /// </summary>
    public bool Put(IntPtr windowXId)
    {
        var gc = XCreateGC(_deferredDisplay, windowXId, 0, IntPtr.Zero);
        var status = XShmPutImage(_deferredDisplay, windowXId, gc, (XImage*)_shmImage, 0, 0, 0, 0, (uint)Size.Width,
            (uint)Size.Height, send_event: true);
        XFlush(_deferredDisplay);
        XFreeGC(_deferredDisplay, gc);
        return status != 0;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;
        
        // Teardown order per the MIT-SHM spec: detach the server, destroy the image, then drop the segment.
        // https://xorg.freedesktop.org/archive/X11R7.7/doc/xextproto/shm.html
        
        XShmDetach(_deferredDisplay, _shmSegmentInfo);
        
        // Clear data first so XDestroyImage frees only the XImage structure - the shm segment is ours to drop
        // below; otherwise XDestroyImage would call free() on the shmat() pointer.
        _shmImage->data = IntPtr.Zero;
        XDestroyImage(_shmImage);
        _shmImage = null;
        
        shmdt(_shmSegmentInfo->shmaddr);
        shmctl(_shmSegmentInfo->shmid, IPC_RMID, IntPtr.Zero);

        Marshal.FreeHGlobal((IntPtr)_shmSegmentInfo);
        _shmSegmentInfo = null;

        Logger.TryGet(LogEventLevel.Debug, LogArea.X11Platform)?.Log(this, "[X11ShmImage] Dispose");
    }
}
