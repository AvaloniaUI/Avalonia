using System;
using Avalonia.OpenGL.Egl;
using Avalonia.Platform;
using Avalonia.Win32.DirectX;
using Avalonia.Win32.Interop;
using MicroCom.Runtime;

namespace Avalonia.Win32.WinRT.Composition
{
    internal class WinUiCompositedWindowSurface : IDirect3D11TexturePlatformSurface, IDisposable, ICompositionEffectsSurface
    {
        private readonly WinUiCompositionShared _shared;
        private readonly EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo _info;
        private WinUiCompositedWindow? _window;
        private BlurEffect _blurEffect;

        public WinUiCompositedWindowSurface(WinUiCompositionShared shared, EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo info)
        {
            _shared = shared;
            _info = info;
        }

        public IDirect3D11TextureRenderTarget CreateRenderTarget(IPlatformGraphicsContext context, IntPtr d3dDevice)
        {
            var cornerRadius = AvaloniaLocator.Current.GetService<Win32PlatformOptions>()
                ?.WinUICompositionBackdropCornerRadius;
            _window ??= new WinUiCompositedWindow(_info, _shared, cornerRadius);
            _window.SetBlur(_blurEffect);

            return new WinUiCompositedWindowRenderTarget(context, _window, d3dDevice, _shared.Compositor);
        }

        public void Dispose()
        {
            _window?.Dispose();
            _window = null;
        }

        public bool IsBlurSupported(BlurEffect effect) => effect switch
        {
            BlurEffect.None => true,
            BlurEffect.Acrylic => Win32Platform.WindowsVersion >= WinUiCompositionShared.MinAcrylicVersion,
            BlurEffect.MicaLight => Win32Platform.WindowsVersion >= WinUiCompositionShared.MinHostBackdropVersion,
            BlurEffect.MicaDark => Win32Platform.WindowsVersion >= WinUiCompositionShared.MinHostBackdropVersion,
            _ => false
        };

        public void SetBlur(BlurEffect enable)
        {
            _blurEffect = enable;
            _window?.SetBlur(enable);
        }
    }

    internal class WinUiCompositedWindowRenderTarget : IDirect3D11TextureRenderTarget
    {
        private static readonly Guid IID_ID3D11Texture2D = Guid.Parse("6f15aaf2-d208-4e89-9ab4-489535d34f9c");

        private readonly IPlatformGraphicsContext _context;
        private readonly WinUiCompositedWindow _window;
        private readonly IUnknown _d3dDevice;
        private readonly ICompositor _compositor;
        private readonly ICompositorInterop _interop;
        private readonly ICompositionGraphicsDevice _compositionDevice;
        private readonly ICompositionGraphicsDevice2 _compositionDevice2;
        private readonly ICompositionSurface _surface;
        private PixelSize _size;
        private bool _lost;
        private readonly ICompositionDrawingSurfaceInterop _surfaceInterop;
        private readonly ICompositionDrawingSurface _drawingSurface;

        public WinUiCompositedWindowRenderTarget(IPlatformGraphicsContext context,
            WinUiCompositedWindow window, IntPtr device,
            ICompositor compositor)
        {
            _context = context;
            _window = window;

            try
            {
                _d3dDevice = MicroComRuntime.CreateProxyFor<IUnknown>(device, false).CloneReference();
                _compositor = compositor.CloneReference();
                _interop = compositor.QueryInterface<ICompositorInterop>();
                _compositionDevice = _interop.CreateGraphicsDevice(_d3dDevice);
                _compositionDevice2 = _compositionDevice.QueryInterface<ICompositionGraphicsDevice2>();
                _drawingSurface = _compositionDevice2.CreateDrawingSurface2(new UnmanagedMethods.SIZE(),
                    DirectXPixelFormat.B8G8R8A8UIntNormalized, DirectXAlphaMode.Premultiplied);
                _surface = _drawingSurface.QueryInterface<ICompositionSurface>();
                _surfaceInterop = _drawingSurface.QueryInterface<ICompositionDrawingSurfaceInterop>();
            }
            catch
            {
                _surface?.Dispose();
                _surfaceInterop?.Dispose();
                _drawingSurface?.Dispose();
                _compositionDevice2?.Dispose();
                _compositionDevice?.Dispose();
                _interop?.Dispose();
                _compositor?.Dispose();
                _d3dDevice?.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            _surface.Dispose();
            _surfaceInterop.Dispose();
            _drawingSurface.Dispose();
            _compositionDevice2.Dispose();
            _compositionDevice.Dispose();
            _interop.Dispose();
            _compositor.Dispose();
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
                _window.ResizeIfNeeded(size);
                _window.SetSurface(_surface);
                
                void* pTexture;
                UnmanagedMethods.POINT off;
                try
                {
                    if (_size != size)
                    {
                        _surfaceInterop.Resize(new UnmanagedMethods.POINT
                        {
                            X = size.Width,
                            Y = size.Height
                        });
                        _size = size;
                    }
                    var iid = IID_ID3D11Texture2D;
                    off = _surfaceInterop.BeginDraw(null, &iid, &pTexture);
                }
                catch (Exception e)
                {
                    _lost = true;
                    throw new RenderTargetCorruptedException(e);
                }

                needsEndDraw = true;
                var offset = new PixelPoint(off.X, off.Y);
                using var texture = MicroComRuntime.CreateProxyFor<IUnknown>(pTexture, true);

                var session = new Session(_surfaceInterop, texture, transaction, _size, offset, scale);
                transaction = null;
                return session;
            }
            finally
            {
                if (transaction != null)
                {
                    if (needsEndDraw)
                        _surfaceInterop.EndDraw();
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
            private readonly ICompositionDrawingSurfaceInterop _surfaceInterop;
            private readonly IUnknown _texture;

            public Session(ICompositionDrawingSurfaceInterop surfaceInterop, IUnknown texture, IDisposable transaction,
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
}
