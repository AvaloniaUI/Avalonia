using System;
using System.Runtime.InteropServices;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Platform;
using Avalonia.Skia;
using SkiaSharp;

namespace Avalonia.Browser.Skia
{
    internal class BrowserSkiaRasterSurface : IBrowserSkiaSurface, IFramebufferPlatformSurface, IDisposable
    {
        public SKColorType ColorType { get; set; }

        public PixelSize Size { get; set; }

        public double Scaling { get; set; }

        private FramebufferData? _fbData;
        private readonly Action<IntPtr, SKSizeI> _blitCallback;
        private readonly Action _onDisposeAction;

        public BrowserSkiaRasterSurface(
            SKColorType colorType, PixelSize size, double scaling, Action<IntPtr, SKSizeI> blitCallback)
        {
            ColorType = colorType;
            Size = size;
            Scaling = scaling;
            _blitCallback = blitCallback;
            _onDisposeAction = Blit;
        }

        public void Dispose()
        {
            _fbData?.Dispose();
            _fbData = null;
        }

        public ILockedFramebuffer Lock()
        {
            var bytesPerPixel = 4; // TODO: derive from ColorType
            var dpi = Scaling * 96.0;
            var width = (int)(Size.Width * Scaling);
            var height = (int)(Size.Height * Scaling);

            if (_fbData is null || _fbData?.Size.Width != width || _fbData?.Size.Height != height)
            {
                _fbData?.Dispose();
                _fbData = new FramebufferData(width, height, bytesPerPixel);
            }

            var pixelFormat = ColorType.ToPixelFormat();
            var data = _fbData.Value;
            return new LockedFramebuffer(
                data.Address, data.Size, data.RowBytes,
                new Vector(dpi, dpi), pixelFormat, _onDisposeAction);
        }

        private void Blit()
        {
            if (_fbData != null)
            {
                var data = _fbData.Value;
                _blitCallback(data.Address, new SKSizeI(data.Size.Width, data.Size.Height));
            }
        }

        private readonly struct FramebufferData
        {
            public PixelSize Size { get; }

            public int RowBytes { get; }

            public IntPtr Address { get; }

            public FramebufferData(int width, int height, int bytesPerPixel)
            {
                Size = new PixelSize(width, height);
                RowBytes = width * bytesPerPixel;
                Address = Marshal.AllocHGlobal(width * height * bytesPerPixel);
            }

            public void Dispose()
            {
                Marshal.FreeHGlobal(Address);
            }
        }

        public IFramebufferRenderTarget CreateFramebufferRenderTarget() => new FuncFramebufferRenderTarget(Lock);
    }
}
