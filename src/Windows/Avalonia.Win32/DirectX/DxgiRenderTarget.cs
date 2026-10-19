using System;
using Avalonia.OpenGL.Egl;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Platform;
using Avalonia.Win32.OpenGl.Angle;
using MicroCom.Runtime;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32.DirectX
{
    internal unsafe class DxgiRenderTarget : EglPlatformSurfaceRenderTargetBase
    {
        // DXGI_FORMAT_B8G8R8A8_UNORM is target texture format as per ANGLE documentation 

        public const uint DXGI_USAGE_RENDER_TARGET_OUTPUT = 0x00000020U;
        private readonly Guid ID3D11Texture2DGuid = Guid.Parse("6F15AAF2-D208-4E89-9AB4-489535D34F9C");

        private readonly EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo _window;
        private readonly DxgiConnection _connection;
        private readonly IDXGIDevice? _dxgiDevice;
        private readonly IDXGIFactory2? _dxgiFactory;
        private readonly IDXGISwapChain1? _swapChain;
        private readonly uint _flagsUsed;

        private IUnknown? _renderTexture;
        private PixelSize _bufferSize;
        private uint? _backgroundApplied;
        private EglSurface? _surface;

        public DxgiRenderTarget(EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo window, EglContext context, DxgiConnection connection) : base(context)
        {
            _window = window;
            _connection = connection;

            // the D3D device is expected to at least be an ID3D11Device 
            // but how do I wrap an IntPtr as a managed IUnknown now? Like this. 
            IUnknown pdevice = MicroComRuntime.CreateProxyFor<IUnknown>(((AngleWin32EglDisplay)context.Display).GetDirect3DDevice(), false);

            _dxgiDevice = pdevice.QueryInterface<IDXGIDevice>();

            // only needing the adapter pointer to ask it for the IDXGI Factory 
            using (var adapterPointer = _dxgiDevice.Adapter)
            {
                Guid factoryGuid = MicroComRuntime.GetGuidFor(typeof(IDXGIFactory2));
                _dxgiFactory = MicroComRuntime.CreateProxyFor<IDXGIFactory2>(adapterPointer.GetParent(&factoryGuid), true);
            }

            DXGI_SWAP_CHAIN_DESC1 dxgiSwapChainDesc = new DXGI_SWAP_CHAIN_DESC1();

            // standard swap chain really. 
            dxgiSwapChainDesc.Format = DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM;
            dxgiSwapChainDesc.SampleDesc.Count = 1U;
            dxgiSwapChainDesc.SampleDesc.Quality = 0U;
            dxgiSwapChainDesc.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
            dxgiSwapChainDesc.AlphaMode = DXGI_ALPHA_MODE.DXGI_ALPHA_MODE_IGNORE;
            dxgiSwapChainDesc.Width = (uint)_window.Size.Width;
            dxgiSwapChainDesc.Height = (uint)_window.Size.Height;
            dxgiSwapChainDesc.BufferCount = 2U;
            dxgiSwapChainDesc.SwapEffect = DXGI_SWAP_EFFECT.DXGI_SWAP_EFFECT_FLIP_DISCARD;

            // A buffer that does not match the window yet (after WM_SIZE, before the next present)
            // is shown at the top left as it is, never stretched: stretching blurs and shifts the
            // whole window for a frame while it is resized. The area it leaves uncovered shows
            // the swap chain's background color.
            dxgiSwapChainDesc.Scaling = DXGI_SCALING.DXGI_SCALING_NONE;

            // okay I know this looks bad, but we're hitting our render-calls by awaiting via dxgi 
            // this is done in the DxgiConnection itself 
            _flagsUsed = dxgiSwapChainDesc.Flags = (uint)(DXGI_SWAP_CHAIN_FLAG.DXGI_SWAP_CHAIN_FLAG_ALLOW_TEARING);

            _swapChain = _dxgiFactory.CreateSwapChainForHwnd
            (
                    _dxgiDevice,
                    window.Handle,
                    &dxgiSwapChainDesc,
                    null,
                    null
            );

            _dxgiFactory.MakeWindowAssociation(window.Handle, (uint)(DXGI_MWA.DXGI_MWA_NO_ALT_ENTER | DXGI_MWA.DXGI_MWA_NO_PRINT_SCREEN));

            _bufferSize = new PixelSize((int)dxgiSwapChainDesc.Width, (int)dxgiSwapChainDesc.Height);
        }

        /// <inheritdoc />
        public override IGlPlatformSurfaceRenderingSession BeginDrawCore(IRenderTarget.RenderTargetSceneInfo sceneInfo)
        {
            if (_swapChain is null)
            {
                throw new InvalidOperationException("No chain to draw on");
            }

            var contextLock = Context.EnsureCurrent();
            IDisposable? transaction = null;
            var success = false;
            try
            {
                ApplyBackground(sceneInfo.BackgroundColor);

                // The buffer takes the size of the frame being drawn, not the window's current
                // client size: while the window is resized it runs ahead of the frame, and a
                // buffer larger than the frame would show an undrawn strip along its edges.
                var size = sceneInfo.Size;
                if (size.Width <= 0 || size.Height <= 0)
                    size = _window.Size;
                if (size != _bufferSize)
                {
                    _bufferSize = size;

                    if (_renderTexture is not null)
                    {
                        _surface?.Dispose();
                        _surface = null;

                        _renderTexture.Dispose();
                        _renderTexture = null;
                    }

                    _swapChain.ResizeBuffers(2,
                        (ushort)size.Width,
                        (ushort)size.Height,
                        DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM,
                        (ushort)_flagsUsed
                        );
                }

                // Get swapchain texture here 
                var texture = _renderTexture;
                if (texture is null)
                {
                    _surface?.Dispose();
                    _surface = null;

                    Guid textureGuid = ID3D11Texture2DGuid;
                    texture = MicroComRuntime.CreateProxyFor<IUnknown>(_swapChain.GetBuffer(0, &textureGuid), true);
                }
                _renderTexture = texture;

                if (_surface is null)
                {
                    // I also have to get the pointer to this texture directly 
                    _surface = ((AngleWin32EglDisplay)Context.Display).WrapDirect3D11Texture(MicroComRuntime.GetNativeIntPtr(_renderTexture),
                        0, 0, size.Width, size.Height);
                }

                var res = base.BeginDraw(_surface, size, _window.Scaling, () =>
                {
                    _swapChain.Present((ushort)0U, (ushort)0U);
                    transaction?.Dispose();
                    contextLock?.Dispose();
                }, true);
                success = true;
                return res;
            }
            finally
            {
                if (!success)
                {
                    _surface?.Dispose();
                    _surface = null;
                    if (_renderTexture is not null)
                    {
                        _renderTexture.Dispose();
                        _renderTexture = null;
                    }
                    transaction?.Dispose();
                    contextLock.Dispose();
                }
            }
        }

        private void ApplyBackground(Avalonia.Media.Color background)
        {
            // A transparent color means the scene has no opaque background: black, the swap chain's default.
            var argb = background.A == 0 ? 0xFF000000u : background.ToUInt32();
            if (argb == _backgroundApplied)
                return;
            _backgroundApplied = argb;
            var color = new DXGI_RGBA
            {
                r = ((argb >> 16) & 0xFF) / 255f,
                g = ((argb >> 8) & 0xFF) / 255f,
                b = (argb & 0xFF) / 255f,
                a = 1f,
            };
            _swapChain!.SetBackgroundColor(&color);
        }

        public override void Dispose()
        {
            base.Dispose();
            _dxgiDevice?.Dispose();
            _dxgiFactory?.Dispose();
            _swapChain?.Dispose();
            _surface?.Dispose();
            _renderTexture?.Dispose();
        }
    }
}
