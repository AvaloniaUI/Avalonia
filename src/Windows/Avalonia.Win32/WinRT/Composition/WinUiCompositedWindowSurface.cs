using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Avalonia.Media;
using Avalonia.OpenGL.Egl;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Avalonia.Win32.DirectX;
using Avalonia.Win32.Interop;
using MicroCom.Runtime;

namespace Avalonia.Win32.WinRT.Composition
{
    internal class WinUiCompositedWindowSurface : IDirect3D11TexturePlatformSurface, IDirect3D11TexturePlatformSurface2, IDisposable, ICompositionEffectsSurface
    {
        private readonly WinUiCompositionShared _shared;
        private readonly EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo _info;
        private WinUiCompositedWindow? _window;
        private BlurEffect _blurEffect;
        private WinUiCompositedWindowRenderTarget? _renderTarget;

        public WinUiCompositedWindowSurface(WinUiCompositionShared shared, EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo info)
        {
            _shared = shared;
            _info = info;
        }

        IDirect3D11TextureRenderTarget IDirect3D11TexturePlatformSurface.CreateRenderTarget(IPlatformGraphicsContext context, IntPtr d3dDevice)
        {
           return (IDirect3D11TextureRenderTarget) CreateRenderTarget(context, d3dDevice);
        }

        /// <summary>
        /// The color space of the most recently created render target, which is the presented one.
        /// </summary>
        public PresentationColorSpace CurrentColorSpace => _renderTarget?.ColorSpace ?? PresentationColorSpace.Unspecified;

        /// <summary>
        /// Raised on the UI thread when <see cref="CurrentColorSpace"/> changes, forwarded from
        /// whichever render target is current — see <see cref="WinUiCompositedWindowRenderTarget"/>'s
        /// own <c>ColorSpaceChanged</c>, which is where the actual color space is known, since it is
        /// only resolved lazily on the first (or a later re-created) surface, not at construction.
        /// </summary>
        public event EventHandler? CurrentColorSpaceChanged;

        public IDirect3D11TextureRenderTarget2 CreateRenderTarget(IPlatformGraphicsContext context, IntPtr d3dDevice)
        {
            var cornerRadius = AvaloniaLocator.Current.GetService<Win32PlatformOptions>()
                ?.WinUICompositionBackdropCornerRadius;
            _window ??= new WinUiCompositedWindow(_info, _shared, cornerRadius);
            _window.SetBlur(_blurEffect);

            if (_renderTarget != null)
                _renderTarget.ColorSpaceChanged -= OnRenderTargetColorSpaceChanged;
            _renderTarget = new WinUiCompositedWindowRenderTarget(context, _window, d3dDevice, _shared.Compositor);
            _renderTarget.ColorSpaceChanged += OnRenderTargetColorSpaceChanged;
            return _renderTarget;
        }

        private void OnRenderTargetColorSpaceChanged(object? sender, EventArgs e) =>
            // Render targets are created (and surfaces re-created) off the UI thread, but the event
            // is for application code.
            Dispatcher.UIThread.Post(() => CurrentColorSpaceChanged?.Invoke(this, EventArgs.Empty));

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

    internal class WinUiCompositedWindowRenderTarget : IDirect3D11TextureRenderTarget, IDirect3D11TextureRenderTarget2,
        IColorManagedRenderTarget
    {
        private static readonly Guid IID_ID3D11Texture2D = Guid.Parse("6f15aaf2-d208-4e89-9ab4-489535d34f9c");

        private readonly IPlatformGraphicsContext _context;
        private readonly WinUiCompositedWindow _window;
        private readonly IUnknown _d3dDevice;
        private readonly ICompositor _compositor;
        private readonly ICompositorInterop _interop;
        private readonly ICompositionGraphicsDevice _compositionDevice;
        private readonly ICompositionGraphicsDevice2 _compositionDevice2;
        private ICompositionSurface? _surface;
        private PixelSize _size;
        private bool _lost;
        private bool _isSurfaceSupportTransparency;
        private ICompositionDrawingSurfaceInterop? _surfaceInterop;
        private ICompositionDrawingSurface? _drawingSurface;
        private readonly PresentationColorSpace _preferredColorSpace;
        private readonly bool _wantsScRgb;
        private PresentationColorSpace _colorSpace;

        /// <summary>
        /// The color space which was really applied. Unspecified until the surface was created, and
        /// also when the requested one could not be used.
        /// </summary>
        public PresentationColorSpace ColorSpace
        {
            get => _colorSpace;
            private set
            {
                if (_colorSpace == value)
                    return;
                _colorSpace = value;
                ColorSpaceChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Raised when <see cref="ColorSpace"/> changes after a (re-)create, so
        /// <see cref="WinUiCompositedWindowSurface"/> can forward it as
        /// <see cref="IColorManagedPresentation.CurrentColorSpaceChanged"/>. Not itself part of
        /// <see cref="IColorManagedRenderTarget"/> — that interface only reports a static value,
        /// because on every other platform it does not need to change after construction; here it
        /// can, since the surface is created lazily on the first <see cref="CreateSurface"/> rather
        /// than known up front.
        /// </summary>
        public event EventHandler? ColorSpaceChanged;

        public WinUiCompositedWindowRenderTarget(IPlatformGraphicsContext context,
            WinUiCompositedWindow window, IntPtr device,
            ICompositor compositor)
        {
            _context = context;
            _window = window;
            _preferredColorSpace = AvaloniaLocator.Current.GetService<PresentationOptions>()?.PreferredColorSpace
                                   ?? PresentationColorSpace.Unspecified;
            // WideGamut resolves to scRGB here the same way it resolves to DisplayP3 on Metal --
            // Windows has no Display P3 composition format, so scRGB is the concrete answer.
            _wantsScRgb = _preferredColorSpace is PresentationColorSpace.ScRgb or PresentationColorSpace.WideGamut;

            try
            {
                _d3dDevice = MicroComRuntime.CreateProxyFor<IUnknown>(device, false).CloneReference();
                _compositor = compositor.CloneReference();
                _interop = compositor.QueryInterface<ICompositorInterop>();
                _compositionDevice = _interop.CreateGraphicsDevice(_d3dDevice);
                _compositionDevice2 = _compositionDevice.QueryInterface<ICompositionGraphicsDevice2>();
            }
            catch
            {
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
            _surface?.Dispose();
            _surfaceInterop?.Dispose();
            _drawingSurface?.Dispose();
            _compositionDevice2.Dispose();
            _compositionDevice.Dispose();
            _interop.Dispose();
            _compositor.Dispose();
            _d3dDevice.Dispose();
        }

        [MemberNotNull(nameof(_drawingSurface), nameof(_surface), nameof(_surfaceInterop))]
        private void CreateSurface(in IRenderTarget.RenderTargetSceneInfo sceneInfo)
        {
            bool isTransparency = sceneInfo.TransparencyLevel != CompositionTransparencyLevel.None;
            var surfaceSize = sceneInfo.Size;

            // Do not use Premultiplied when the window is not Transparency. Because the Premultiplied AlphaMode will increase the performance loss of DWM. See https://github.com/AvaloniaUI/Avalonia/issues/20643
            var alphaMode = isTransparency ? DirectXAlphaMode.Premultiplied : DirectXAlphaMode.Ignore;

            var size = new UnmanagedMethods.SIZE { X = surfaceSize.Width, Y = surfaceSize.Height };

            // scRGB keeps values outside of 0..1, so it needs a float surface. Windows has no
            // Display P3 composition format, so the pixel format is the only wide gamut lever here.
            ICompositionDrawingSurface? drawingSurface = null;
            if (_wantsScRgb)
            {
                try
                {
                    drawingSurface = _compositionDevice2.CreateDrawingSurface2(
                        size, DirectXPixelFormat.R16G16B16A16Float, alphaMode);
                    ColorSpace = PresentationColorSpace.ScRgb;
                }
                catch (COMException)
                {
                    // The device can not give us a float surface. Present unmanaged rather than
                    // failing the render target, and report it so the application can adapt.
                    drawingSurface = null;
                }
            }

            if (drawingSurface is null)
            {
                drawingSurface = _compositionDevice2.CreateDrawingSurface2(
                    size, DirectXPixelFormat.B8G8R8A8UIntNormalized, alphaMode);
                ColorSpace = PresentationColorSpace.Unspecified;
            }

            _drawingSurface = drawingSurface;
            _surface = _drawingSurface.QueryInterface<ICompositionSurface>();
            _surfaceInterop = _drawingSurface.QueryInterface<ICompositionDrawingSurfaceInterop>();

            _isSurfaceSupportTransparency = isTransparency;
            _size = surfaceSize;
        }

        public PlatformRenderTargetState State =>
            _context.IsLost || _lost ? PlatformRenderTargetState.Corrupted : PlatformRenderTargetState.Ready;

        IDirect3D11TextureRenderTargetRenderSession IDirect3D11TextureRenderTarget.BeginDraw()
        {
            var fallbackSceneInfo = new IRenderTarget.RenderTargetSceneInfo(_window.WindowInfo.Size,
                _window.WindowInfo.Scaling, CompositionTransparencyLevel.None);
            return BeginDraw(fallbackSceneInfo);
        }

        public unsafe IDirect3D11TextureRenderTargetRenderSession BeginDraw(
            IRenderTarget.RenderTargetSceneInfo sceneInfo)
        {
            if (State.IsCorrupted)
                throw new RenderTargetCorruptedException();
            var transaction = _window.BeginTransaction();

            bool needsEndDraw = false;
            try
            {
                bool isTransparency = sceneInfo.TransparencyLevel != CompositionTransparencyLevel.None;
               
                if (_surface is null || _surfaceInterop is null || _drawingSurface is null || _isSurfaceSupportTransparency != isTransparency)
                {
                    // Re-create the surface with correct alpha mode if the transparency support is not correct. This can happen when the transparency level is changed.
                    _surface?.Dispose();
                    _surfaceInterop?.Dispose();
                    _drawingSurface?.Dispose();

                    CreateSurface(in sceneInfo);
                }

                var size = sceneInfo.Size;
                var scale = sceneInfo.Scaling;
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
                        _surfaceInterop?.EndDraw();
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
