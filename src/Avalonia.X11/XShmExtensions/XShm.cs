using System;
using System.Runtime.InteropServices;
using ShmSeg = System.UInt64;

namespace Avalonia.X11.XShmExtensions;

internal unsafe class XShm
{
    [DllImport("libXext.so.6", SetLastError = true)]
    public static extern int XShmQueryExtension(IntPtr display);

    [DllImport("libXext.so.6", SetLastError = true)]
    public static extern int XShmQueryVersion(IntPtr display, out int major, out int minor, out bool pixmaps);

    [DllImport("libXext.so.6", SetLastError = true)]
    public static extern int XShmPutImage(IntPtr display, IntPtr drawable, IntPtr gc, XImage* image, int src_x, int src_y,
        int dst_x, int dst_y, uint src_width, uint src_height, bool send_event);

    [DllImport("libXext.so.6", SetLastError = true)]
    public static extern int XShmAttach(IntPtr display, XShmSegmentInfo* shminfo);

    [DllImport("libXext.so.6", SetLastError = true)]
    public static extern int XShmDetach(IntPtr display, XShmSegmentInfo* shminfo);

    [DllImport("libXext.so.6", SetLastError = true)]
    public static extern IntPtr XShmCreateImage(IntPtr display, IntPtr visual, uint depth, int format, IntPtr data,
        XShmSegmentInfo* shminfo, uint width, uint height);
}

[StructLayout(LayoutKind.Sequential)]
unsafe struct XShmSegmentInfo
{
    public ShmSeg shmseg; /* resource id */
    public int shmid; /* kernel id */
    public char* shmaddr; /* address in client */
    public bool readOnly; /* how the server should attach it */

    public override string ToString()
    {
        return
            $"XShmSegmentInfo {{ shmseg = {shmseg}, shmid = {shmid}, shmaddr = {new IntPtr(shmaddr).ToString("X")}, readOnly = {readOnly} }}";
    }
}

[StructLayout(LayoutKind.Sequential)]
struct XShmCompletionEvent
{
    public XEventName type; /* of event */
    public ulong serial; /* # of last request processed by server */
    public bool send_event; /* true if this came from a SendEvent request */
    public IntPtr display; /* Display the event was read from */
    public IntPtr drawable; /* drawable of request */

    public int major_code; /* Know as ShmReqCode, the expected value is MIT-SHM 130 */
    public int minor_code; /* Know as X_ShmPutImage, the expected value is X_ShmPutImage 3*/
    public ShmSeg shmseg; /* the ShmSeg used in the request */
    public ulong offset; /* the offset into ShmSeg used in the request */
}
