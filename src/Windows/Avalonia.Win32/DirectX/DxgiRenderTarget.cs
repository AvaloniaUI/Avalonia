using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia.OpenGL.Angle;
using Avalonia.OpenGL.Egl;
using Avalonia.OpenGL.Surfaces;
using MicroCom.Runtime;
using static Avalonia.OpenGL.Egl.EglGlPlatformSurfaceBase;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32.DxgiSwapchain
{
#pragma warning disable CA1416 // Validate platform compatibility, if you enter this not on windows you have messed up badly 
#nullable enable
    public unsafe class DxgiRenderTarget : EglPlatformSurfaceRenderTargetBase
    {
        // DXGI_FORMAT_B8G8R8A8_UNORM is target texture format as per ANGLE documentation 

        public const uint DXGI_USAGE_RENDER_TARGET_OUTPUT = 0x00000020U;

        private IEglWindowGlPlatformSurfaceInfo _window;
        private EglPlatformOpenGlInterface _egl;
        private DxgiConnection _connection;
        private IDXGIDevice? _dxgiDevice = null;
        private IDXGIFactory2? _dxgiFactory = null;
        private IDXGISwapChain1? _swapChain = null;
        private IUnknown? _renderTexture = null;

        private Interop.UnmanagedMethods.RECT _clientRect = default;

        private uint _flagsUsed;

        private Guid ID3D11Texture2DGuid = Guid.Parse("6F15AAF2-D208-4E89-9AB4-489535D34F9C");

        public DxgiRenderTarget(IEglWindowGlPlatformSurfaceInfo window, EglPlatformOpenGlInterface egl, DxgiConnection connection) : base(egl)
        {
            _window = window;
            _egl = egl;
            _connection = connection;

            // the D3D device is expected to at least be an ID3D11Device 
            // but how do I wrap an IntPtr as a managed IUnknown now? Like this. 
            IUnknown pdevice = MicroComRuntime.CreateProxyFor<IUnknown>(((AngleWin32EglDisplay)_egl.Display).GetDirect3DDevice(), false);

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

            Interop.UnmanagedMethods.RECT pClientRect;
            GetClientRect(_window.Handle, out pClientRect);
            _clientRect = pClientRect;
        }

        public override IGlPlatformSurfaceRenderingSession BeginDraw()
        {
            if (_swapChain is null)
            {
                throw new InvalidOperationException("No chain to draw on");
            }

            var contextLock = _egl.PrimaryContext.EnsureCurrent();
            EglSurface? surface = null;
            IDisposable? transaction = null;
            var success = false;
            try
            {
                Interop.UnmanagedMethods.RECT pClientRect;
                GetClientRect(_window.Handle, out pClientRect);
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
                surface = ((AngleWin32EglDisplay)_egl.Display).WrapDirect3D11Texture(_egl, MicroComRuntime.GetNativeIntPtr(_renderTexture),
                    0, 0, size.Width, size.Height);

                var res = base.BeginDraw(surface, _window, () =>
                {
                    _swapChain.Present((ushort)0U, (ushort)0U);
                    surface?.Dispose();
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
#pragma warning restore CA1416 // Validate platform compatibility
#nullable restore
}
