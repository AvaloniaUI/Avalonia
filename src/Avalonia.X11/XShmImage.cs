using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Logging;
using Avalonia.Platform;
using static Avalonia.X11.XLib;

namespace Avalonia.X11
{
    unsafe class XShmImage : IDisposable
    {
        private readonly XShmManager _mgr;
        private readonly IntPtr _display;
        private XShmSegmentInfo* _shmSegment;
        private XImage* _image;
        private bool _shmAttached;
        private bool _x11Attached;
        public bool IsFree { get; set; } = true;
        public int Width { get; }
        public int Height { get; }
        public IntPtr ShmSeg { get; }
        
        [DllImport("libc", SetLastError = true)]
        private static extern int shmget(IntPtr key, IntPtr size, int shmflg);

        [DllImport("libc", SetLastError = true)]
        private static extern IntPtr shmat(int shmid, IntPtr shmaddr, int shmflg);
        
        
        [DllImport("libc", SetLastError = true)]
        private static extern IntPtr shmdt(IntPtr shmaddr);

        
        
        public XShmImage(XShmManager mgr, IntPtr display, IntPtr visual, uint depth, int width, int height)
        {
            _mgr = mgr;
            _display = display;

            width = Math.Max(width, 1);
            height = Math.Max(height, 1);
            Width = width;
            Height = height;

            if (_image != null && (_image->width != width || _image->height != height))
                Dispose();

            if (_image == null)
            {
                _shmSegment = (XShmSegmentInfo*)Marshal.AllocHGlobal(Marshal.SizeOf<XShmSegmentInfo>());
                *_shmSegment = default;
                _image = XShmCreateImage( _display, visual, depth, 2,
                    IntPtr.Zero, _shmSegment, (uint)width, (uint)height);
                if (_image == null)
                {
                    Dispose();
                    throw new X11Exception("Unable to allocate SHM image");
                }

                var id = shmget(IntPtr.Zero, new IntPtr(_image->bytes_per_line * _image->height),
                    1023);
                if (id == 0)
                {
                    Dispose();
                    throw new X11Exception("shmget failed " + Marshal.GetLastWin32Error());
                }

                _shmSegment->shmid = id;
                _shmSegment->shmaddr = _image->data = shmat(_shmSegment->shmid, IntPtr.Zero, 0);
                _shmSegment->readOnly = 0;

                if (_shmSegment->shmaddr == IntPtr.Zero || _shmSegment->shmaddr == new IntPtr(-1))
                {
                    Dispose();
                    throw new X11Exception("shmat failed " + Marshal.GetLastWin32Error());
                }

                _shmAttached = true;
                
                if (!XShmAttach(_display, _shmSegment))
                {
                    Dispose();
                    throw new X11Exception("XShmAttach failed");
                }

                _x11Attached = true;

                ShmSeg = _shmSegment->shmseg;
            }
        }

        public ILockedFramebuffer LockForDraw(IntPtr xid, double scaling)
        {
            IsFree = false;
            return new LockedFramebuffer(_image->data, new PixelSize(_image->width, _image->height),
                _image->bytes_per_line, new Vector(96, 96) * scaling, PixelFormat.Bgra8888,
                () =>
                {

                    XLockDisplay(_display);
                    var gc = XCreateGC(_display, xid, 0, IntPtr.Zero);
                    XShmPutImage(_display, xid, gc, _image, 0, 0, 0, 0,
                        (uint)_image->width, (uint)_image->height, true);
                    XFreeGC(_display, gc);
                    XFlush(_display);
                    XUnlockDisplay(_display);

                });
        }

        ~XShmImage()
        {
            Logger.TryGet(LogEventLevel.Error, "X11")
                ?.Log("XShmImage", "Leaked XShmImage instance");
            Dispose();
        }
        
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            _mgr.Unregister(this);
            XLockDisplay(_display);

            if (_x11Attached)
            {
                _x11Attached = false;
                XShmDetach(_display, _shmSegment);
            }
            
            if (_image != null)
            {
                XDestroyImage(_image);
                _image = null;
            }

            if (_shmAttached)
            {
                shmdt(_shmSegment->shmaddr);
                _shmAttached = false;
            }

            if (_shmSegment != null)
            {
                Marshal.FreeHGlobal((IntPtr)_shmSegment);
                _shmSegment = null;
            }

            XUnlockDisplay(_display);
        }
    }

    class XShmSwapChain : IDisposable, IFramebufferPlatformSurface
    {
        private readonly XShmManager _mgr;
        private readonly IntPtr _xid;
        private readonly IntPtr _visual;
        private readonly uint _depth;
        private readonly int _buffers;
        private readonly Func<double> _scaling;
        private Queue<XShmImage> _queue = new Queue<XShmImage>();

        public XShmSwapChain(XShmManager mgr, IntPtr xid, XVisualInfo? visual, int buffers,
            Func<double> scaling)
        {
            _mgr = mgr;
            _xid = xid;
            _buffers = buffers;
            _scaling = scaling;
            if (visual != null)
            {
                _visual = visual.Value.visual;
                _depth = visual.Value.depth;
            }
            else
            {
                _visual = XDefaultVisual(mgr.Display, 0);
                _depth = 32;
            }
        }

        public ILockedFramebuffer Lock()
        {
            XLockDisplay(_mgr.Display);
            try
            {
                var scaling = _scaling();
                XLockDisplay(_mgr.Display);
                XGetGeometry(_mgr.Display, _xid, out var _, out var _, out var _, out var width, out var height,
                    out var _, out var _);
                XUnlockDisplay(_mgr.Display);
                width = Math.Max(width, 1);
                height = Math.Max(height, 1);

                _mgr.Pump();
                XShmImage image = null;
                // Attempt to reuse a buffer if
                if (
                    // queue is not empty
                    _queue.Count != 0
                    // and
                    && (
                        // the next buffer is free
                        _queue.Peek().IsFree
                        // or if we've run out of available buffers
                        || _queue.Count >= _buffers))
                    image = _queue.Dequeue();

                if (image != null && (image.Width != width || image.Height != height))
                {
                    image.Dispose();
                    image = null;
                }

                if (image == null)
                    image = _mgr.Create(width, height, _visual, _depth);

                _mgr.WaitForAvailability(image);

                // Add the image to the end of the queue
                _queue.Enqueue(image);

                // And use it as the render target
                return image.LockForDraw(_xid, scaling);
            }
            finally
            {
                XUnlockDisplay(_mgr.Display);
            }
        }

        public void Dispose()
        {
            XLockDisplay(_mgr.Display);
            while (_queue.Count>0)
                _queue.Dequeue().Dispose();
            XUnlockDisplay(_mgr.Display);
        }
    }
    
    class XShmManager
    {
        private readonly X11Info _x11Info;
        public IntPtr Display { get; }

        private ConcurrentDictionary<IntPtr, WeakReference<XShmImage>> _images =
            new ConcurrentDictionary<IntPtr, WeakReference<XShmImage>>();

        public XShmManager(X11Info x11Info)
        {
            _x11Info = x11Info;
            Display = _x11Info.ShmDisplay;
        }

        public XShmImage Create(int width, int height, IntPtr visual, uint depth)
        {
            var image = new XShmImage(this, Display, visual, depth, width, height);
            _images[image.ShmSeg] = new WeakReference<XShmImage>(image);
            return image;
        }

        public void Unregister(XShmImage shmseg)
        {
            Pump(shmseg);
            _images.TryRemove(shmseg.ShmSeg, out var _);
        }

        private void Pump(XShmImage waitFor)
        {
            XLockDisplay(Display);
            try
            {
                while (waitFor?.IsFree == false || XPending(Display) != 0)
                {
                    XNextEvent(Display, out var ev);
                    if (ev.type == (XEventName)_x11Info.ShmEventBase)
                    {
                        if (!_images.TryGetValue(ev.ShmCompletionEvent.shmseg, out var image))
                            Logger.TryGet(LogEventLevel.Error, "X11")
                                ?.Log("XShmImage", "Unknown shmseg in completion event");
                        else if (image.TryGetTarget(out var target))
                            target.IsFree = true;
                    }
                }
            }
            finally
            {
                XUnlockDisplay(Display);
            }
        }

        public void WaitForAvailability(XShmImage image) => Pump(image);
        public void Pump() => Pump(null);
    }
}
