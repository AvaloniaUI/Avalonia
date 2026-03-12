using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

using Avalonia.Controls;
using Avalonia.OpenGL.Egl;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Platform;
using Avalonia.Win32.DComposition;
using Avalonia.Win32.Interop;
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
        private readonly IDXGIFactory2 _dxgiFactory;
        private IDXGISwapChain1? _swapChain;
        private DXGI_SWAP_CHAIN_FLAG _dxgiSwapChainDescFlagsUsed;
        private const uint SwapChainDescBufferCount = 2;

        private IUnknown? _renderTexture;
        private PixelSize _size;
        private EglSurface? _surface;

        public DxgiRenderTarget(EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo window, EglContext context,
            DxgiConnection connection, WindowTransparencyLevel transparencyLevel) : base(context)
        {
            _window = window;
            _connection = connection;
            _transparencyLevel = transparencyLevel;

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

            CreateSurface(window.Size);

            _dxgiFactory.MakeWindowAssociation(window.Handle, (uint)(DXGI_MWA.DXGI_MWA_NO_ALT_ENTER | DXGI_MWA.DXGI_MWA_NO_PRINT_SCREEN));
        }

        private IDCompositionDesktopDevice? _compositionDesktopDevice;
        private IDCompositionTarget? _compositionTarget;

        [MemberNotNull(nameof(_swapChain))]
        private void CreateSurface(PixelSize expectedPixelSize)
        {
            _swapChain?.Dispose();
            _swapChain = null;

            _compositionDesktopDevice?.Dispose();
            _compositionDesktopDevice = null;

            _compositionTarget?.Dispose();
            _compositionTarget = null;

            _surface?.Dispose();
            _surface = null;

            _renderTexture?.Dispose();
            _renderTexture = null;

            var windowInfo = _window;
            var size = expectedPixelSize;

            DXGI_SWAP_CHAIN_DESC1 dxgiSwapChainDesc = new DXGI_SWAP_CHAIN_DESC1();

            // standard swap chain really. 
            dxgiSwapChainDesc.Format = DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM;
            dxgiSwapChainDesc.SampleDesc.Count = 1U;
            dxgiSwapChainDesc.SampleDesc.Quality = 0U;
            dxgiSwapChainDesc.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
            dxgiSwapChainDesc.AlphaMode = DXGI_ALPHA_MODE.DXGI_ALPHA_MODE_IGNORE;
            dxgiSwapChainDesc.Width = (uint)size.Width;
            dxgiSwapChainDesc.Height = (uint)size.Height;
            dxgiSwapChainDesc.BufferCount = SwapChainDescBufferCount;
            dxgiSwapChainDesc.SwapEffect = DXGI_SWAP_EFFECT.DXGI_SWAP_EFFECT_FLIP_DISCARD;

            _dxgiSwapChainDescFlagsUsed = DXGI_SWAP_CHAIN_FLAG.DXGI_SWAP_CHAIN_FLAG_ALLOW_TEARING;
            dxgiSwapChainDesc.Flags = (uint)_dxgiSwapChainDescFlagsUsed;

            if (IsTransparency && DxgiConnection.IsTransparencySupported())
            {
                dxgiSwapChainDesc.AlphaMode = DXGI_ALPHA_MODE.DXGI_ALPHA_MODE_PREMULTIPLIED;

                _swapChain = _dxgiFactory.CreateSwapChainForComposition(_dxgiDevice, &dxgiSwapChainDesc, null);

                Guid IID_IDCompositionDesktopDevice = Guid.Parse("5f4633fe-1e08-4cb8-8c75-ce24333f5602");
                var result = NativeMethods.DCompositionCreateDevice2(default, IID_IDCompositionDesktopDevice, out var cDevice);
                if (result != UnmanagedMethods.HRESULT.S_OK)
                {
                    throw new Win32Exception((int)result);
                }

                var device = MicroComRuntime.CreateProxyFor<IDCompositionDesktopDevice>(cDevice, ownsHandle: true);
                _compositionDesktopDevice = device;
                using IDCompositionVisual compositionVisual =
                    device.CreateTargetForHwnd(windowInfo.Handle, topmost: true);
                var compositionTarget = compositionVisual.QueryInterface<IDCompositionTarget>();
                _compositionTarget = compositionTarget;
                IDCompositionVisual container = device.CreateVisual();
                container.SetContent(_swapChain);
                compositionTarget.SetRoot(container);
                device.Commit();
            }
            else
            {
                _swapChain = _dxgiFactory.CreateSwapChainForHwnd
                (
                    _dxgiDevice,
                    windowInfo.Handle,
                    &dxgiSwapChainDesc,
                    null,
                    null
                );
            }

            _size = size;
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
                var size = sceneInfo.Size;
                var scale = sceneInfo.Scaling;

                var shouldTransparency = IsTransparency && DxgiConnection.IsTransparencySupported();
                var isSupportTransparency = _swapChain.Desc1.AlphaMode is DXGI_ALPHA_MODE.DXGI_ALPHA_MODE_PREMULTIPLIED or DXGI_ALPHA_MODE.DXGI_ALPHA_MODE_STRAIGHT;

                if (shouldTransparency != isSupportTransparency)
                {
                    CreateSurface(size);
                }

                if (_size != size)
                {
                    // we gotta resize
                    if (_renderTexture is not null)
                    {
                        _surface?.Dispose();
                        _surface = null;

                        _renderTexture.Dispose();
                        _renderTexture = null;
                    }

                    _swapChain.ResizeBuffers(SwapChainDescBufferCount,
                        (ushort)size.Width, (ushort)size.Height,
                        DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM,
                        (uint)_dxgiSwapChainDescFlagsUsed
                        );
                    _size = size;
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

                var res = base.BeginDraw(_surface, size, scale, () =>
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

        public override void Dispose()
        {
            base.Dispose();
            _dxgiDevice?.Dispose();
            _dxgiFactory?.Dispose();
            _swapChain?.Dispose();
            _surface?.Dispose();
            _renderTexture?.Dispose();

            _compositionDesktopDevice?.Dispose();
            _compositionTarget?.Dispose();
        }

        internal static bool RectsEqual(in RECT l, in RECT r)
        {
            return (l.left == r.left)
                && (l.top == r.top)
                && (l.right == r.right)
                && (l.bottom == r.bottom);
        }

        public bool IsTransparency => _transparencyLevel != WindowTransparencyLevel.None;

        public void SetTransparencyLevel(WindowTransparencyLevel transparencyLevel)
        {
            _transparencyLevel = transparencyLevel;
        }

        private WindowTransparencyLevel _transparencyLevel;
    }
}
