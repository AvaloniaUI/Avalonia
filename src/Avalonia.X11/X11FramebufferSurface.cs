using System;
using Avalonia.Platform.Surfaces;
using Avalonia.Platform;
using static Avalonia.X11.XLib;
namespace Avalonia.X11
{
    internal class X11FramebufferSurface : IFramebufferPlatformSurface
    {
        private readonly IntPtr _display;
        private readonly X11Window.SurfaceInfo _info;
        private readonly int _depth;
        private readonly bool _retain;
        private RetainedFramebuffer? _fb;

        public X11FramebufferSurface(IntPtr display, X11Window.SurfaceInfo info, int depth, bool retain)
        {
            _display = display;
            _info = info;
            _depth = depth;
            _retain = retain;
        }

        void Blit(RetainedFramebuffer fb)
        {
            var image = new XImage();
            int bitsPerPixel = 32;
            image.width = fb.Size.Width;
            image.height = fb.Size.Height;
            image.format = 2; //ZPixmap;
            image.data = fb.Address;
            image.byte_order = 0;// LSBFirst;
            image.bitmap_unit = bitsPerPixel;
            image.bitmap_bit_order = 0;// LSBFirst;
            image.bitmap_pad = bitsPerPixel;
            image.depth = _depth;
            image.bytes_per_line = fb.RowBytes;
            image.bits_per_pixel = bitsPerPixel;
            XLockDisplay(_display);
            XInitImage(ref image);
            var gc = XCreateGC(_display, _info.Handle, 0, IntPtr.Zero);
            XPutImage(_display, _info.Handle, gc, ref image, 0, 0, 0, 0, (uint)fb.Size.Width, (uint)fb.Size.Height);
            XFreeGC(_display, gc);
            XSync(_display, true);
            XUnlockDisplay(_display);
            if (!_retain)
            {
                _fb?.Dispose();
                _fb = null;
            }
        }
        
        public ILockedFramebuffer Lock(IRenderTarget.RenderTargetSceneInfo sceneInfo, out FramebufferLockProperties properties)
        {
            _info.UpdateGtkFrameExtents(_display, sceneInfo.ShadowExtents);
            
            XLockDisplay(_display);
            XGetGeometry(_display, _info.Handle, out var root, out var x, out var y, out var width, out var height,
                out var bw, out var d);
            XUnlockDisplay(_display);

            var framebufferValid = (_fb != null && _fb.Size.Width == width && _fb.Size.Height == height);
            if (!framebufferValid)
            {
                _fb?.Dispose();
                _fb = null;
                _fb = new RetainedFramebuffer(new PixelSize(width, height), PixelFormat.Bgra8888, AlphaFormat.Premul);
            }

            properties = new FramebufferLockProperties(framebufferValid);
            return _fb!.Lock(new Vector(96, 96), Blit);
        }

        public IFramebufferRenderTarget CreateFramebufferRenderTarget() => new FuncFramebufferRenderTarget(Lock, _retain);
    }
}
