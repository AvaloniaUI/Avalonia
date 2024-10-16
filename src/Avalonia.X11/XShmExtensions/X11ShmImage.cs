using System;
using System.Diagnostics;
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
    public X11ShmImage(PixelSize size, X11ShmImageManager x11ShmImageManager)
    {
        ShmImageManager = x11ShmImageManager;
        // The XShmSegmentInfo struct will store in XImage, and it must pin the address.
        IntPtr pXShmSegmentInfo = Marshal.AllocHGlobal(Marshal.SizeOf<XShmSegmentInfo>());
        var pShmSegmentInfo = (XShmSegmentInfo*)pXShmSegmentInfo;
        PShmSegmentInfo = pShmSegmentInfo;

        var context = x11ShmImageManager.Context;
        var display = context.Display;
        var visual = context.Visual;

        const int ZPixmap = 2;

        var width = size.Width;
        var height = size.Height;

        Size = size;

        var depth = context.Depth;
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

        X11ShmDebugLogger.WriteLine($"[X11ShmImage] CreateX11ShmImage Size={Size} shmid={shmid:X} shmaddr={shmaddr}");
    }

    public X11ShmImageManager ShmImageManager { get; }

    public XImage* ShmImage { get; set; }
    public XShmSegmentInfo* PShmSegmentInfo { get; }
    public IntPtr ShmAddr => new IntPtr(PShmSegmentInfo->shmaddr);

    public const int ByteSizeOfPixel = 4;

    public PixelSize Size { get; }

    public ShmSeg ShmSeg => PShmSegmentInfo->shmseg;

    public void Dispose()
    {
        var context = ShmImageManager.Context;
        XShmDetach(context.Display, PShmSegmentInfo);

        shmdt(ShmAddr);
        shmctl(PShmSegmentInfo->shmid, IPC_RMID, IntPtr.Zero);

        Marshal.FreeHGlobal(new IntPtr(PShmSegmentInfo));

        X11ShmDebugLogger.WriteLine($"[X11ShmImage] Dispose");
    }
}
