using System;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.LinuxFramebuffer.Output;
using Avalonia.Platform;

namespace Avalonia.LinuxFramebuffer
{
    public sealed unsafe class FbdevOutput : IFramebufferPlatformSurface, IDisposable, IOutputBackend
    {
        private int _fd;
        private fb_fix_screeninfo _fixedInfo;
        private fb_var_screeninfo _varInfo;
        private IntPtr _mappedLength;
        private IntPtr _mappedAddress;
        private FbDevBackBuffer _backBuffer;
        public double Scaling { get; set; }

        /// <summary>
        /// Create a Linux frame buffer device output
        /// </summary>
        /// <param name="fileName">The frame buffer device name.
        /// Defaults to the value in environment variable FRAMEBUFFER or /dev/fb0 when FRAMEBUFFER is not set</param>
        public FbdevOutput(string fileName = null) : this(fileName, null)
        {
        }

        /// <summary>
        /// Create a Linux frame buffer device output
        /// </summary>
        /// <param name="fileName">The frame buffer device name.
        /// Defaults to the value in environment variable FRAMEBUFFER or /dev/fb0 when FRAMEBUFFER is not set</param>
        /// <param name="format">The required pixel format for the frame buffer.
        /// A null value will leave the frame buffer in the current pixel format.
        /// Otherwise sets the frame buffer to the required format</param>
        public FbdevOutput(string fileName, PixelFormat? format)
        {
            fileName ??= Environment.GetEnvironmentVariable("FRAMEBUFFER") ?? "/dev/fb0";
            _fd = NativeUnsafeMethods.open(fileName, 2, 0);
            if (_fd <= 0)
                throw new Exception("Error: " + Marshal.GetLastWin32Error());

            try
            {
                Init(format);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        void Init(PixelFormat? format)
        {
            fixed (void* pnfo = &_varInfo)
            {
                if (-1 == NativeUnsafeMethods.ioctl(_fd, FbIoCtl.FBIOGET_VSCREENINFO, pnfo))
                    throw new Exception("FBIOGET_VSCREENINFO error: " + Marshal.GetLastWin32Error());

                if (format.HasValue)
                {
                    SetBpp(format.Value);

                    if (-1 == NativeUnsafeMethods.ioctl(_fd, FbIoCtl.FBIOPUT_VSCREENINFO, pnfo))
                        _varInfo.transp = new fb_bitfield();

                    NativeUnsafeMethods.ioctl(_fd, FbIoCtl.FBIOPUT_VSCREENINFO, pnfo);

                    if (-1 == NativeUnsafeMethods.ioctl(_fd, FbIoCtl.FBIOGET_VSCREENINFO, pnfo))
                        throw new Exception("FBIOGET_VSCREENINFO error: " + Marshal.GetLastWin32Error());

                    if (_varInfo.bits_per_pixel != 32)
                        throw new Exception("Unable to set 32-bit display mode");
                }
            }
            fixed(void*pnfo = &_fixedInfo)
                if (-1 == NativeUnsafeMethods.ioctl(_fd, FbIoCtl.FBIOGET_FSCREENINFO, pnfo))
                    throw new Exception("FBIOGET_FSCREENINFO error: " + Marshal.GetLastWin32Error());

            _mappedLength = new IntPtr(_fixedInfo.line_length * _varInfo.yres);
            _mappedAddress = NativeUnsafeMethods.mmap(IntPtr.Zero, _mappedLength, 3, 1, _fd, IntPtr.Zero);
            if (_mappedAddress == new IntPtr(-1))
                throw new Exception($"Unable to mmap {_mappedLength} bytes, error {Marshal.GetLastWin32Error()}");
            fixed (fb_fix_screeninfo* pnfo = &_fixedInfo)
            {
                int idlen;
                for (idlen = 0; idlen < 16 && pnfo->id[idlen] != 0; idlen++) ;
                Id = Encoding.ASCII.GetString(pnfo->id, idlen);
            }
        }

        void SetBpp(PixelFormat format)
        {
            if (format == PixelFormat.Rgba8888)
            {
                _varInfo.bits_per_pixel = 32;
                _varInfo.grayscale = 0;
                _varInfo.red = _varInfo.blue = _varInfo.green = _varInfo.transp = new fb_bitfield
                {
                    length = 8
                };
                _varInfo.green.offset = 8;
                _varInfo.blue.offset = 16;
                _varInfo.transp.offset = 24;
            }
            else if (format == PixelFormat.Bgra8888)
            {
                _varInfo.bits_per_pixel = 32;
                _varInfo.grayscale = 0;
                _varInfo.red = _varInfo.blue = _varInfo.green = _varInfo.transp = new fb_bitfield
                {
                    length = 8
                };
                _varInfo.green.offset = 8;
                _varInfo.red.offset = 16;
                _varInfo.transp.offset = 24;
            }
            else if (format == PixelFormat.Rgb565)
            {
                _varInfo.bits_per_pixel = 16;
                _varInfo.grayscale = 0;
                _varInfo.red = _varInfo.blue = _varInfo.green = _varInfo.transp = new fb_bitfield();
                _varInfo.red.length = 5;
                _varInfo.green.offset = 5;
                _varInfo.green.length = 6;
                _varInfo.blue.offset = 11;
                _varInfo.blue.length = 5;
            }
            else throw new NotSupportedException($"Pixel format {format} is not supported");
        }

        public string Id { get; private set; }

        public PixelSize PixelSize
        {
            get
            {
                fb_var_screeninfo nfo;
                if (-1 == NativeUnsafeMethods.ioctl(_fd, FbIoCtl.FBIOGET_VSCREENINFO, &nfo))
                    throw new Exception("FBIOGET_VSCREENINFO error: " + Marshal.GetLastWin32Error());
                return new PixelSize((int)nfo.xres, (int)nfo.yres);
            }
        }

        public ILockedFramebuffer Lock()
        {
            if (_fd <= 0)
                throw new ObjectDisposedException("LinuxFramebuffer");
            return (_backBuffer ??=
                    new FbDevBackBuffer(_fd, _fixedInfo, _varInfo, _mappedAddress))
                .Lock(new Vector(96, 96) * Scaling);
        }
        
        public IFramebufferRenderTarget CreateFramebufferRenderTarget() => new FuncFramebufferRenderTarget(Lock);


        private void ReleaseUnmanagedResources()
        {
            if (_mappedAddress != IntPtr.Zero)
            {
                NativeUnsafeMethods.munmap(_mappedAddress, _mappedLength);
                _mappedAddress = IntPtr.Zero;
            }
            if(_fd == 0)
                return;
            NativeUnsafeMethods.close(_fd);
            _fd = 0;
        }

        public void Dispose()
        {
            _backBuffer?.Dispose();
            _backBuffer = null;
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~FbdevOutput()
        {
            ReleaseUnmanagedResources();
        }
    }
}
