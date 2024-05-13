using System;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Avalonia.Wayland
{
    internal class WlIconData : IWindowIconImpl, IFramebufferPlatformSurface
    {
        private readonly int _width;
        private readonly int _height;
        private readonly uint[]? _bdata;

        public WlIconData(IBitmap bitmap)
        {
            _width = Math.Min(bitmap.PixelSize.Width, 128);
            _height = Math.Min(bitmap.PixelSize.Height, 128);
            _bdata = new uint[_width * _height];
            using var cpuContext = AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>().CreateBackendContext(null);
            using var rt = cpuContext.CreateRenderTarget(new[] { this } );
            using var ctx = rt.CreateDrawingContext();
            ctx.DrawBitmap(bitmap.PlatformImpl.Item, 1, new Rect(bitmap.Size), new Rect(0, 0, _width, _height));
            Data = new uint[_width * _height + 2];
            Data[0] = (uint)_width;
            Data[1] = (uint)_height;
            for (var y = 0; y < _height; y++)
            {
                var r = y * _width;
                for (var x = 0; x < _width; x++)
                    Data[r + x + 2] = _bdata[r + x];
            }

            _bdata = null;
        }

        internal uint[] Data { get; }

        public unsafe void Save(Stream outputStream)
        {
            using var wr = new WriteableBitmap(new PixelSize(_width, _height), new Vector(96, 96), PixelFormat.Bgra8888);
            using var fb = wr.Lock();
            var fbp = (uint*)fb.Address;
            for (var y = 0; y < _height; y++)
            {
                var r = y * _width;
                var fbr = y * fb.RowBytes / 4;
                for (var x = 0; x < _width; x++)
                    fbp[fbr + x] = Data[r + x + 2];
            }

            wr.Save(outputStream);
        }

        public ILockedFramebuffer Lock()
        {
            var h = GCHandle.Alloc(_bdata, GCHandleType.Pinned);
            return new LockedFramebuffer(h.AddrOfPinnedObject(), new PixelSize(_width, _height), _width * 4, new Vector(96, 96), PixelFormat.Bgra8888, () => h.Free());
        }

        public IFramebufferRenderTarget CreateFramebufferRenderTarget() => new FuncFramebufferRenderTarget(Lock);
    }
}
