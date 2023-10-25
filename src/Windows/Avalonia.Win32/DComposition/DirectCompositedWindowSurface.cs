using System;
using System.ComponentModel;
using Avalonia.OpenGL.Egl;
using Avalonia.Platform;
using Avalonia.Win32.DirectX;
using Avalonia.Win32.Interop;
using MicroCom.Runtime;

namespace Avalonia.Win32.DComposition;

internal class DirectCompositedWindowSurface : IDirect3D11TexturePlatformSurface, IDisposable, ICompositionEffectsSurface
{
    private readonly EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo _info;
    private readonly DirectCompositionShared _shared;
    private DirectCompositedWindow? _window;
    private BlurEffect _blurEffect;

    public DirectCompositedWindowSurface(DirectCompositionShared shared, EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo info)
    {
        _shared = shared;
        _info = info;
    }

    public IDirect3D11TextureRenderTarget CreateRenderTarget(IPlatformGraphicsContext context, IntPtr d3dDevice)
    {
        _window ??= new DirectCompositedWindow(_info, _shared);
        SetBlur(_blurEffect);

        return new DirectCompositedWindowRenderTarget(context, d3dDevice, _shared, _window);
    }

    public void Dispose()
    {
        _window?.Dispose();
        _window = null;
    }

    // TODO: we can implement BlurEffect.GaussianBlur in with IDCompositionDevice3.CreateGaussianBlurEffect. 
    public bool IsBlurSupported(BlurEffect effect) => effect == BlurEffect.None;

    public void SetBlur(BlurEffect enable)
    {
        _blurEffect = enable;
        // _window?.SetBlur(enable);
    }
}

internal class DirectCompositedWindowRenderTarget : IDirect3D11TextureRenderTarget
{
    private static readonly Guid IID_ID3D11Texture2D = Guid.Parse("6f15aaf2-d208-4e89-9ab4-489535d34f9c");

    private readonly IPlatformGraphicsContext _context;
    private readonly DirectCompositedWindow _window;
    private readonly IDCompositionVirtualSurface _surface;
    private bool _lost;
    private PixelSize _size;
    private readonly IUnknown _d3dDevice;

    public DirectCompositedWindowRenderTarget(
        IPlatformGraphicsContext context, IntPtr d3dDevice,
        DirectCompositionShared shared, DirectCompositedWindow window)
    {
        _d3dDevice = MicroComRuntime.CreateProxyFor<IUnknown>(d3dDevice, false).CloneReference();

        _context = context;
        _window = window;

        using (var surfaceFactory = shared.Device.CreateSurfaceFactory(_d3dDevice))
        {
            _surface = surfaceFactory.CreateVirtualSurface(1, 1, DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM,
                DXGI_ALPHA_MODE.DXGI_ALPHA_MODE_PREMULTIPLIED);
        }
    }

    public void Dispose()
    {
        _surface.Dispose();
        _d3dDevice.Dispose();
    }

    public bool IsCorrupted => _context.IsLost || _lost;

    public unsafe IDirect3D11TextureRenderTargetRenderSession BeginDraw()
    {
        if (IsCorrupted)
            throw new RenderTargetCorruptedException();
        var transaction = _window.BeginTransaction();
        bool needsEndDraw = false;
        try
        {
            var size = _window.WindowInfo.Size;
            var scale = _window.WindowInfo.Scaling;
            if (_size != size)
            {
                _surface.Resize((ushort)size.Width, (ushort)size.Height);
                _size = size;
            }

            _window.SetSurface(_surface);
                
            void* pTexture;
            UnmanagedMethods.POINT off;
            try
            {
                var rect = new UnmanagedMethods.RECT { right = size.Width, bottom = size.Height };
                var iid = IID_ID3D11Texture2D;
                off = _surface.BeginDraw(&rect, &iid, &pTexture);
            }
            catch (Exception e)
            {
                _lost = true;
                throw new RenderTargetCorruptedException(e);
            }

            needsEndDraw = true;
            var offset = new PixelPoint(off.X, off.Y);
            using var texture = MicroComRuntime.CreateProxyFor<IUnknown>(pTexture, true);

            var session = new Session(_surface, texture, transaction, size, offset, scale);
            transaction = null;
            return session;
        }
        finally
        {
            if (transaction != null)
            {
                if (needsEndDraw)
                    _surface.EndDraw();
                transaction.Dispose();
            }
        }
    }

    private class Session : IDirect3D11TextureRenderTargetRenderSession
    {
        private readonly IDisposable _transaction;
        private readonly PixelSize _size;
        private readonly PixelPoint _offset;
        private readonly double _scaling;
        private readonly IDCompositionSurface _surfaceInterop;
        private readonly IUnknown _texture;

        public Session(IDCompositionSurface surfaceInterop, IUnknown texture, IDisposable transaction,
            PixelSize size, PixelPoint offset, double scaling)
        {
            _transaction = transaction;
            _size = size;
            _offset = offset;
            _scaling = scaling;
            _surfaceInterop = surfaceInterop.CloneReference();
            _texture = texture.CloneReference();
        }

        public void Dispose()
        {
            try
            {
                _texture.Dispose();
                _surfaceInterop.EndDraw();
                _surfaceInterop.Dispose();
            }
            finally
            {
                _transaction.Dispose();
            }
        }

        public IntPtr D3D11Texture2D => _texture.GetNativeIntPtr();
        public PixelSize Size => _size;
        public PixelPoint Offset => _offset;
        public double Scaling => _scaling;
    }
}
