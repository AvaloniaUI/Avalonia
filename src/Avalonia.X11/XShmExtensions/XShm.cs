using System;
using System.Runtime.InteropServices;
using ShmSeg = System.UInt64;

namespace Avalonia.X11.XShmExtensions;

internal unsafe class XShm
{
    [DllImport("libXext.so.6", SetLastError = true)]
    public static extern int XShmQueryExtension(IntPtr display);

    /*
    Status XShmQueryVersion (display, major, minor, pixmaps)
      Display *display;
      int *major, *minor;
      Bool *pixmaps
    */
    [DllImport("libXext.so.6", SetLastError = true)]
    public static extern int XShmQueryVersion(IntPtr display, out int major, out int minor, out bool pixmaps);

    [DllImport("libXext.so.6", SetLastError = true)]
    public static extern int XShmPutImage(IntPtr display, IntPtr drawable, IntPtr gc, XImage* image, int src_x, int src_y,
        int dst_x, int dst_y, uint src_width, uint src_height, bool send_event);

    // XShmAttach(display, &shminfo);
    [DllImport("libXext.so.6", SetLastError = true)]
    public static extern int XShmAttach(IntPtr display, XShmSegmentInfo* shminfo);

    /*
    XImage *XShmCreateImage (display, visual, depth, format, data,
                       shminfo, width, height)
      Display *display;
      Visual *visual;
      unsigned int depth, width, height;
      int format;
      char *data;
      XShmSegmentInfo *shminfo;
    */
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

/*
typedef struct {
       int type;               /* of event * /
       unsigned long serial;   /* # of last request processed by server* /
       Bool send_event;        /* true if this came from a SendEvent request* /
       Display *display;       /* Display the event was read from * /
       Drawable drawable;      /* drawable of request * /
       int major_code;         /* ShmReqCode * /
       int minor_code;         /* X_ShmPutImage * /
       ShmSeg shmseg;          /* the ShmSeg used in the request* /
       unsigned long offset;   /* the offset into ShmSeg used in the request* /
   } XShmCompletionEvent;
 */

[StructLayout(LayoutKind.Sequential)]
struct XShmCompletionEvent
{
    public XEventName type; /* of event */
    public ulong serial; /* # of last request processed by server */
    public bool send_event; /* true if this came from a SendEvent request */
    public IntPtr display; /* Display the event was read from */
    public IntPtr drawable; /* drawable of request */

    public int major_code; /* ShmReqCode 预期是 MIT-SHM 130 的值*/
    public int minor_code; /* X_ShmPutImage 预期是 X_ShmPutImage 3 的值*/
    public ShmSeg shmseg; /* the ShmSeg used in the request */
    public ulong offset; /* the offset into ShmSeg used in the request */
}
