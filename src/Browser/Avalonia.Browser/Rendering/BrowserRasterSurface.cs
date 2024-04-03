using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using Avalonia.Browser.Interop;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Platform;

namespace Avalonia.Browser.Skia;

internal sealed class BrowserRasterSurface : BrowserSurface, IFramebufferPlatformSurface
{
    public PixelFormat PixelFormat { get; set; }

    private FramebufferData? _fbData;
    private readonly Action _onDisposeAction;
    private readonly int _bytesPerPixel;

    public BrowserRasterSurface(JSObject canvasSurface, PixelFormat pixelFormat, BrowserRenderingMode renderingMode)
        : base(canvasSurface, renderingMode)
    {
        PixelFormat = pixelFormat;
        _onDisposeAction = Blit;
        _bytesPerPixel = pixelFormat.BitsPerPixel / 8;
    }

    public override void Dispose()
    {
        _fbData?.Dispose();
        _fbData = null;

        base.Dispose();
    }

    public ILockedFramebuffer Lock()
    {
        var bytesPerPixel = _bytesPerPixel;
        var dpi = Scaling * 96.0;
        var size = RenderSize;

        if (_fbData is null || _fbData?.Size != size)
        {
            _fbData?.Dispose();
            _fbData = new FramebufferData(size.Width, size.Height, bytesPerPixel);
        }

        var data = _fbData;
        return new LockedFramebuffer(
            data.Address, data.Size, data.RowBytes,
            new Vector(dpi, dpi), PixelFormat, _onDisposeAction);
    }

    private void Blit()
    {
        if (_fbData is { } data)
        {
            CanvasHelper.PutPixelData(JsSurface, data.AsSegment, data.Size.Width, data.Size.Height);
        }
    }

    private class FramebufferData
    {
        private static ArrayPool<byte> s_pool = ArrayPool<byte>.Create();

        private readonly byte[] _array;
        private GCHandle _handle;

        public FramebufferData(int width, int height, int bytesPerPixel)
        {
            Size = new PixelSize(width, height);
            RowBytes = width * bytesPerPixel;

            var length = width * height * bytesPerPixel;
            _array = s_pool.Rent(length);

            _handle = GCHandle.Alloc(_array, GCHandleType.Pinned);
            Address = _handle.AddrOfPinnedObject();

            AsSegment = new ArraySegment<byte>(_array, 0, length);
        }

        public PixelSize Size { get; }

        public int RowBytes { get; }

        public IntPtr Address { get; }

        public ArraySegment<byte> AsSegment { get; }

        public void Dispose()
        {
            _handle.Free();
            s_pool.Return(_array);
        }
    }

    public IFramebufferRenderTarget CreateFramebufferRenderTarget() => new FuncFramebufferRenderTarget(Lock);
}
