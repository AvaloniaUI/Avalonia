using System;
using System.Diagnostics;
using Avalonia.Logging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShmSeg = System.UInt64;
using static Avalonia.X11.LibC;
using static Avalonia.X11.XShmExtensions.XShm;
using System.Runtime.InteropServices;

namespace Avalonia.X11.XShmExtensions;

internal unsafe class X11ShmImage : IDisposable
{
    public X11ShmImage(PixelSize size, X11ShmImageSwapchain owner)
    {
        Owner = owner;
        // The XShmSegmentInfo struct will store in XImage, and it must pin the address.
        IntPtr pXShmSegmentInfo = Marshal.AllocHGlobal(Marshal.SizeOf<XShmSegmentInfo>());
        var pShmSegmentInfo = (XShmSegmentInfo*)pXShmSegmentInfo;
        PShmSegmentInfo = pShmSegmentInfo;

        var display = owner.DeferredDisplay;
        var visual = owner.Visual;

        const int ZPixmap = 2;

        var width = size.Width;
        var height = size.Height;

        Size = size;

        var depth = owner.Depth;
        Debug.Assert(depth is 32, "The PixelFormat must be Bgra8888, so that the depth should be 32.");

        IntPtr data = IntPtr.Zero;

        var shmImage = (XImage*)XShmCreateImage(display, visual, (uint)depth, ZPixmap, data, pShmSegmentInfo,
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

        pShmSegmentInfo->shmaddr = (char*)shmaddr.ToPointer();
        shmImage->data = data = shmaddr;

        XShmAttach(display, pShmSegmentInfo);

        Logger.TryGet(LogEventLevel.Debug, LogArea.X11Platform)?.Log(this, "[X11ShmImage] CreateX11ShmImage Size={Size} shmid={Shmid:X} shmaddr={ShmAddr}", Size, shmid, shmaddr);
    }

    public X11ShmImageSwapchain Owner { get; }

    public XImage* ShmImage { get; set; }
    public XShmSegmentInfo* PShmSegmentInfo { get; }
    public IntPtr ShmAddr => new IntPtr(PShmSegmentInfo->shmaddr);

    public const int ByteSizeOfPixel = 4;

    public PixelSize Size { get; }

    public ShmSeg ShmSeg => PShmSegmentInfo->shmseg;

    public void Dispose()
    {
        XShmDetach(Owner.DeferredDisplay, PShmSegmentInfo);

        shmdt(ShmAddr);
        shmctl(PShmSegmentInfo->shmid, IPC_RMID, IntPtr.Zero);

        Marshal.FreeHGlobal(new IntPtr(PShmSegmentInfo));

        Logger.TryGet(LogEventLevel.Debug, LogArea.X11Platform)?.Log(this, "[X11ShmImage] Dispose");
    }
}
