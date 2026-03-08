using System;
using System.Runtime.InteropServices;
using global::Avalonia;
using global::Avalonia.OpenGL;
using global::Avalonia.OpenGL.Egl;
using global::Avalonia.OpenGL.Surfaces;
using global::Avalonia.Platform;
using global::Avalonia.Win32.DirectX;
using global::Avalonia.Win32.OpenGl.Angle;
using MicroCom.Runtime;

namespace Avalonia.WinUI;

internal unsafe class SwapChainGlSurface : EglGlPlatformSurfaceBase
{
    // QI for IDXGISwapChain2 fails on some Windows builds even though
    // IDXGISwapChain3/4 succeed. Use IDXGISwapChain3 which inherits from
    // IDXGISwapChain2 and has SetMatrixTransform at the same vtable slot.
    private static readonly Guid IDXGISwapChain3Guid = new("94d99bdb-f1f8-4ab0-b236-7da0170edab1");

    [DllImport("dxgi.dll", ExactSpelling = true)]
    private static extern int CreateDXGIFactory2(uint Flags, in Guid riid, out IntPtr ppFactory);

    private readonly Func<PixelSize> _getSizeFunc;
    private readonly Func<double> _getScalingFunc;
    private readonly Action<IntPtr> _setSwapChainCallback;
    private IDXGISwapChain1? _swapChain;
    private IntPtr _swapChain3Ptr;

    public SwapChainGlSurface(
        Func<PixelSize> getSizeFunc,
        Func<double> getScalingFunc,
        Action<IntPtr> setSwapChainCallback)
    {
        _getSizeFunc = getSizeFunc;
        _getScalingFunc = getScalingFunc;
        _setSwapChainCallback = setSwapChainCallback;
    }

    public override IGlPlatformSurfaceRenderTarget CreateGlRenderTarget(IGlContext context)
    {
        var eglContext = (EglContext)context;

        if (_swapChain is null)
        {
            _swapChain = CreateSwapChain(eglContext);

            var swapChainPtr = MicroComRuntime.GetNativeIntPtr(_swapChain);
            var guid = IDXGISwapChain3Guid;
            var qiHr = Marshal.QueryInterface(swapChainPtr, in guid, out _swapChain3Ptr);
            if (qiHr != 0 || _swapChain3Ptr == IntPtr.Zero)
                throw new InvalidOperationException(
                    $"QI for IDXGISwapChain3 failed: HR=0x{qiHr:X8}, ptr={_swapChain3Ptr}");

            SetInverseScaleTransform(_getScalingFunc());

            _setSwapChainCallback(swapChainPtr);
        }

        return new SwapChainGlRenderTarget(eglContext, _swapChain, _getSizeFunc, _getScalingFunc, _swapChain3Ptr, this);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DXGI_MATRIX_3X2_F
    {
        public float _11, _12;
        public float _21, _22;
        public float _31, _32;
    }

    internal void SetInverseScaleTransform(double scaling)
    {
        if (_swapChain3Ptr == IntPtr.Zero || scaling <= 0)
            return;

        var inverseScale = new DXGI_MATRIX_3X2_F
        {
            _11 = 1.0f / (float)scaling,
            _22 = 1.0f / (float)scaling
        };

        // IDXGISwapChain2::SetMatrixTransform vtable slot:
        // IUnknown(3) + IDXGIObject(4) + IDXGIDeviceSubObject(1) +
        // IDXGISwapChain(10) + IDXGISwapChain1(11) +
        // SetSourceSize, GetSourceSize, SetMaximumFrameLatency,
        // GetMaximumFrameLatency, GetFrameLatencyWaitableObject = 5
        // → SetMatrixTransform is at slot 34
        var vtable = *(IntPtr**)_swapChain3Ptr;
        var setMatrixTransform = (delegate* unmanaged[Stdcall]<IntPtr, DXGI_MATRIX_3X2_F*, int>)vtable[34];
        var hr = setMatrixTransform(_swapChain3Ptr, &inverseScale);
        Marshal.ThrowExceptionForHR(hr);
    }

    private IDXGISwapChain1 CreateSwapChain(EglContext eglContext)
    {
        var eglDisplay = (AngleWin32EglDisplay)eglContext.Display;
        var d3dDevicePtr = eglDisplay.GetDirect3DDevice();
        var d3dDevice = MicroComRuntime.CreateProxyFor<IUnknown>(d3dDevicePtr, false);

        IDXGIDevice dxgiDevice;
        using (d3dDevice)
            dxgiDevice = d3dDevice.QueryInterface<IDXGIDevice>();

        Guid factoryGuid = MicroComRuntime.GetGuidFor(typeof(IDXGIFactory2));
        var hr = CreateDXGIFactory2(0, in factoryGuid, out var factoryPtr);
        Marshal.ThrowExceptionForHR(hr);
        var dxgiFactory = MicroComRuntime.CreateProxyFor<IDXGIFactory2>(factoryPtr, true);

        var pixelSize = _getSizeFunc();
        var desc = new DXGI_SWAP_CHAIN_DESC1
        {
            Format = DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM,
            SampleDesc = new DXGI_SAMPLE_DESC { Count = 1, Quality = 0 },
            BufferUsage = DxgiRenderTarget.DXGI_USAGE_RENDER_TARGET_OUTPUT,
            BufferCount = 2,
            SwapEffect = DXGI_SWAP_EFFECT.DXGI_SWAP_EFFECT_FLIP_SEQUENTIAL,
            AlphaMode = DXGI_ALPHA_MODE.DXGI_ALPHA_MODE_PREMULTIPLIED,
            Width = (uint)pixelSize.Width,
            Height = (uint)pixelSize.Height,
            Flags = 0
        };

        var swapChain = dxgiFactory.CreateSwapChainForComposition(dxgiDevice, &desc, null);

        dxgiFactory.Dispose();
        dxgiDevice.Dispose();

        return swapChain;
    }

    public void DisposeSwapChain()
    {
        if (_swapChain3Ptr != IntPtr.Zero)
        {
            Marshal.Release(_swapChain3Ptr);
            _swapChain3Ptr = IntPtr.Zero;
        }
        _swapChain?.Dispose();
        _swapChain = null;
    }
}

internal unsafe class SwapChainGlRenderTarget : EglPlatformSurfaceRenderTargetBase
{
    private static readonly Guid ID3D11Texture2DGuid = Guid.Parse("6F15AAF2-D208-4E89-9AB4-489535D34F9C");

    private readonly IDXGISwapChain1 _swapChain;
    private readonly Func<PixelSize> _getSizeFunc;
    private readonly Func<double> _getScalingFunc;
    private readonly IntPtr _swapChain3Ptr;
    private readonly SwapChainGlSurface _owner;

    private IUnknown? _renderTexture;
    private EglSurface? _surface;
    private PixelSize _lastSize;
    private double _lastScaling;

    public SwapChainGlRenderTarget(
        EglContext context,
        IDXGISwapChain1 swapChain,
        Func<PixelSize> getSizeFunc,
        Func<double> getScalingFunc,
        IntPtr swapChain3Ptr,
        SwapChainGlSurface owner) : base(context)
    {
        _swapChain = swapChain;
        _getSizeFunc = getSizeFunc;
        _getScalingFunc = getScalingFunc;
        _swapChain3Ptr = swapChain3Ptr;
        _owner = owner;
    }

    public override IGlPlatformSurfaceRenderingSession BeginDrawCore(IRenderTarget.RenderTargetSceneInfo sceneInfo)
    {
        var contextLock = Context.EnsureCurrent();
        var success = false;
        try
        {
            var size = _getSizeFunc();
            var scaling = _getScalingFunc();

            if (scaling != _lastScaling)
            {
                _owner.SetInverseScaleTransform(scaling);
                _lastScaling = scaling;
            }

            if (size != _lastSize)
            {
                _surface?.Dispose();
                _surface = null;
                _renderTexture?.Dispose();
                _renderTexture = null;

                _swapChain.ResizeBuffers(2,
                    (ushort)size.Width,
                    (ushort)size.Height,
                    DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM,
                    0);

                _lastSize = size;
            }

            if (_renderTexture is null)
            {
                _surface?.Dispose();
                _surface = null;

                Guid textureGuid = ID3D11Texture2DGuid;
                _renderTexture = MicroComRuntime.CreateProxyFor<IUnknown>(
                    _swapChain.GetBuffer(0, &textureGuid), true);
            }

            if (_surface is null)
            {
                _surface = ((AngleWin32EglDisplay)Context.Display).WrapDirect3D11Texture(
                    MicroComRuntime.GetNativeIntPtr(_renderTexture),
                    0, 0, size.Width, size.Height);
            }

            var res = base.BeginDraw(_surface, size, scaling, () =>
            {
                _swapChain.Present(1, 0);
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
                _renderTexture?.Dispose();
                _renderTexture = null;
                contextLock.Dispose();
            }
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        _surface?.Dispose();
        _renderTexture?.Dispose();
    }
}
