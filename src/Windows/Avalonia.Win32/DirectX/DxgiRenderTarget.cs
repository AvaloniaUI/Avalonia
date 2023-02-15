using System;
using Avalonia.OpenGL.Egl;
using Avalonia.OpenGL.Surfaces;
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
        private RECT _clientRect;

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

            GetClientRect(_window.Handle, out var pClientRect);
            _clientRect = pClientRect;
        }

        /// <inheritdoc />
        public override IGlPlatformSurfaceRenderingSession BeginDrawCore()
        {
            if (_swapChain is null)
            {
                throw new InvalidOperationException("No chain to draw on");
            }

            var contextLock = Context.EnsureCurrent();
            EglSurface? surface = null;
            IDisposable? transaction = null;
            var success = false;
            try
            {
                GetClientRect(_window.Handle, out var pClientRect);
                if (!RectsEqual(pClientRect, _clientRect))
                {
                    // we gotta resize 
                    _clientRect = pClientRect;

                    if (_renderTexture is not null)
                    {
                        _renderTexture.Dispose();
                        _renderTexture = null;
                    }

                    _swapChain.ResizeBuffers(2,
                        (ushort)(pClientRect.right - pClientRect.left),
                        (ushort)(pClientRect.bottom - pClientRect.top),
                        DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM,
                        (ushort)_flagsUsed
                        );
                }

                var size = _window.Size;

                // Get swapchain texture here 
                var texture = _renderTexture;
                if (texture is null)
                {
                    Guid textureGuid = ID3D11Texture2DGuid;
                    texture = MicroComRuntime.CreateProxyFor<IUnknown>(_swapChain.GetBuffer(0, &textureGuid), true);
                }
                _renderTexture = texture;

                // I also have to get the pointer to this texture directly 
                surface = ((AngleWin32EglDisplay)Context.Display).WrapDirect3D11Texture(MicroComRuntime.GetNativeIntPtr(_renderTexture),
                    0, 0, size.Width, size.Height);

                var res = base.BeginDraw(surface, _window.Size, _window.Scaling, () =>
                {
                    _swapChain.Present((ushort)0U, (ushort)0U);
                    surface.Dispose();
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
                    surface?.Dispose();
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

        public override void Dispose()
        {
            base.Dispose();
            _dxgiDevice?.Dispose();
            _dxgiFactory?.Dispose();
            _swapChain?.Dispose();
            _renderTexture?.Dispose();
        }

        internal static bool RectsEqual(in RECT l, in RECT r)
        {
            return (l.left == r.left)
                && (l.top == r.top)
                && (l.right == r.right)
                && (l.bottom == r.bottom);
        }

    }
}
