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
        private IDXGIDevice* _dxgiDevice = null;
        private IDXGIFactory2* _dxgiFactory = null;
        private IDXGISwapChain1* _swapChain = null;
        private ID3D11Texture2D* _renderTexture = null;

        private Interop.UnmanagedMethods.RECT _clientRect = default;

        private uint _flagsUsed;

        public DxgiRenderTarget(IEglWindowGlPlatformSurfaceInfo window, EglPlatformOpenGlInterface egl, DxgiConnection connection) : base(egl)
        {
            _window = window;
            _egl = egl;
            _connection = connection;

            // the D3D device is expected to at least be an ID3D11Device 
            ID3D11Device* pdevice = (ID3D11Device*)((AngleWin32EglDisplay)_egl.Display).GetDirect3DDevice();

            IDXGIDevice* testDevice = null;
            var deviceGuid = IDXGIDevice.Guid;
            HRESULT retval = pdevice->QueryInterface(&deviceGuid, (void**)&testDevice);
            if (retval.FAILED)
            {
                // quite possibly error-not-implemented or error-not-supported 
                throw new Win32Exception((int)retval);
            }
            else
            {
                _dxgiDevice = testDevice;

                IDXGIAdapter* adapterPointer = null;
                IDXGIFactory2* factoryPointer = null;

                retval = _dxgiDevice->GetAdapter(&adapterPointer);
                if (retval.FAILED)
                {
                    throw new Win32Exception((int)retval);
                }
                Guid factoryGuid = IDXGIFactory2.Guid;
                retval = adapterPointer->GetParent(&factoryGuid, (void**)&factoryPointer);
                if (retval.FAILED)
                {
                    throw new Win32Exception((int)retval);
                }
                adapterPointer->Release();

                _dxgiFactory = factoryPointer;

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

                IDXGISwapChain1* pSwapChain = null;


                retval = _dxgiFactory->CreateSwapChainForHwnd
                (
                        _dxgiDevice,
                        window.Handle,
                        &dxgiSwapChainDesc,
                        null,
                        null,
                        &pSwapChain
                );

                if (retval.FAILED)
                {
                    throw new Win32Exception(retval);
                }

                _swapChain = pSwapChain;
                Interop.UnmanagedMethods.RECT pClientRect;
                GetClientRect(_window.Handle, out pClientRect);
                _clientRect = pClientRect;
            }
        }

        public override IGlPlatformSurfaceRenderingSession BeginDraw()
        {
            var contextLock = _egl.PrimaryContext.EnsureCurrent();
            EglSurface? surface = null;
            IDisposable? transaction = null;
            var success = false;
            HRESULT retval = default;
            try
            {
                Interop.UnmanagedMethods.RECT pClientRect;
                GetClientRect(_window.Handle, out pClientRect);
                if (!RectsEqual(pClientRect, _clientRect))
                {
                    // we gotta resize 
                    _clientRect = pClientRect;

                    _renderTexture->Release();
                    _renderTexture = null;

                    retval = _swapChain->ResizeBuffers(2U,
                        (uint)(pClientRect.right - pClientRect.left),
                        (uint)(pClientRect.bottom - pClientRect.top),
                        DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM,
                        _flagsUsed
                        );
                    if (retval.FAILED)
                    {
                        throw new Win32Exception(retval);
                    }
                }

                var size = _window.Size;

                // Get swapchain texture here 
                var texture = _renderTexture;
                if (texture is null)
                {
                    Guid textureGuid = ID3D11Texture2D.Guid;
                    retval = _swapChain->GetBuffer(0, &textureGuid, (void**)&texture);
                    if (retval.FAILED)
                    {
                        // this hasn't happened yet in my testing, but theoretically things can go wrong. 
                        throw new Win32Exception((int)retval);
                    }
                }
                _renderTexture = texture;

                surface = ((AngleWin32EglDisplay)_egl.Display).WrapDirect3D11Texture(_egl, (IntPtr)texture,
                    0, 0, size.Width, size.Height);

                var res = base.BeginDraw(surface, _window, () =>
                {
                    _swapChain->Present(0U, 0U);
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
                        _renderTexture->Release();
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
        }

        ~DxgiRenderTarget()
        {
            // unsafe (native) references only, release them if they're not null. 
            if (_dxgiDevice is not null)
            {
                _dxgiDevice->Release();
                _dxgiDevice = null;
            }
            if (_dxgiFactory is not null)
            {
                _dxgiFactory->Release();
                _dxgiFactory = null;
            }
            if (_swapChain is not null)
            {
                _swapChain->Release();
                _swapChain = null;
            }
            if (_renderTexture is not null)
            {
                _renderTexture->Release();
                _renderTexture = null;
            }
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
