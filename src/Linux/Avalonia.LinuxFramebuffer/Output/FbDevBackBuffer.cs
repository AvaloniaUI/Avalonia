#nullable enable

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Logging;
using Avalonia.Platform;

namespace Avalonia.LinuxFramebuffer.Output
{
    internal unsafe class FbDevBackBuffer : IDisposable
    {
        private readonly int _fb;
        private readonly fb_fix_screeninfo _fixedInfo;
        private readonly fb_var_screeninfo _varInfo;
        private readonly IntPtr _targetAddress;
        private readonly object _lock = new object();
        private readonly AsyncFbBlitter? _asyncBlit;

        class AsyncFbBlitter : IDisposable
        {
            private readonly FbDevBackBuffer _fb;
            private AutoResetEvent _signalToThread = new AutoResetEvent(false);
            private ManualResetEvent _transferCompleted = new ManualResetEvent(true);
            private Thread _thread;
            private volatile bool _exit;

            public AsyncFbBlitter(FbDevBackBuffer fb)
            {
                _fb = fb;
                _thread = new Thread(Worker)
                {
                    IsBackground = true,
                    Name = "FbDevBackBuffer::AsyncBlitter",
                    Priority = ThreadPriority.Highest
                };
                _thread.Start();
            }

            private void Worker()
            {
                while (true)
                {
                    _signalToThread.WaitOne();
                    if (_exit)
                        return;
                    try
                    {
                        _fb.BlitToDevice();
                    }
                    catch(Exception e)
                    {
                        Logger.TryGet(LogEventLevel.Fatal, "FBDEV")?.Log(this, "Unable to update framebuffer: " + e);
                    }

                    _transferCompleted.Set();
                }
            }
            
            public void Dispose()
            {
                _exit = true;
                _signalToThread.Set();
                _thread.Join();
                _signalToThread.Dispose();
                _transferCompleted.Dispose();
            }

            public void WaitForTransfer() => _transferCompleted.WaitOne();

            public void BeginBlit()
            {
                _transferCompleted.Reset();
                _signalToThread.Set();
            }
        }

        public FbDevBackBuffer(int fb, fb_fix_screeninfo fixedInfo, fb_var_screeninfo varInfo, IntPtr targetAddress,
            bool asyncBlit)
        {
            _fb = fb;
            _fixedInfo = fixedInfo;
            _varInfo = varInfo;
            _targetAddress = targetAddress;
            Address = Marshal.AllocHGlobal(RowBytes * Size.Height);
            if (asyncBlit)
                _asyncBlit = new AsyncFbBlitter(this);
        }
        

        public void Dispose()
        {
            if (Address != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(Address);
                Address = IntPtr.Zero;
            }

            if (_asyncBlit != null)
                _asyncBlit.Dispose();
        }

        public static LockedFramebuffer LockFb(IntPtr address, fb_var_screeninfo varInfo,
            fb_fix_screeninfo fixedInfo, Vector dpi, Action? dispose)
        {
            return new LockedFramebuffer(address,
                new PixelSize((int)varInfo.xres, (int)varInfo.yres),
                (int)fixedInfo.line_length, dpi,
                varInfo.bits_per_pixel == 16 ? PixelFormat.Rgb565
                : varInfo.blue.offset == 16 ? PixelFormat.Rgba8888
                : PixelFormat.Bgra8888, dispose);
        }

        private void BlitToDevice()
        {
            NativeUnsafeMethods.ioctl(_fb, FbIoCtl.FBIO_WAITFORVSYNC, null);
            NativeUnsafeMethods.memcpy(_targetAddress, Address, new IntPtr(RowBytes * Size.Height));
        }

        public ILockedFramebuffer Lock(Vector dpi)
        {
            _asyncBlit?.WaitForTransfer();
            Monitor.Enter(_lock);
            try
            {
                return LockFb(Address, _varInfo, _fixedInfo, dpi,
                    () =>
                    {
                        try
                        {
                            if (_asyncBlit != null)
                                _asyncBlit.BeginBlit();
                            else
                                BlitToDevice();
                        }
                        finally
                        {
                            Monitor.Exit(_lock);
                        }
                    });
            }
            catch
            {
                Monitor.Exit(_lock);
                throw;
            }
        }

        public IntPtr Address { get; private set; }
        public PixelSize Size => new PixelSize((int)_varInfo.xres, (int) _varInfo.yres);
        public int RowBytes => (int) _fixedInfo.line_length;
    }
}
