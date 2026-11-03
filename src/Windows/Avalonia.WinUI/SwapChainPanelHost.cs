using System;
using System.Runtime.InteropServices;
using System.Threading;
using global::Avalonia.OpenGL.Egl;
using global::Avalonia.Platform;
using global::Avalonia.Reactive;
using global::Avalonia.Rendering.Composition;
using global::Avalonia.Win32.DirectX;
using MicroCom.Runtime;
using Microsoft.UI.Xaml.Controls;

namespace Avalonia.WinUI;

/// <summary>
/// Displays the composition swapchain rendered by <see cref="CompositionSwapchainRenderTarget"/>
/// inside a WinUI <see cref="SwapChainPanel"/>.
/// </summary>
internal sealed unsafe class SwapChainPanelHost : ISwapchainVisualHost, IDisposable
{
    // QI for IDXGISwapChain2 fails on some Windows builds even though IDXGISwapChain3/4 succeed.
    // IDXGISwapChain3 inherits from IDXGISwapChain2 and has SetMatrixTransform at the same vtable slot.
    private static readonly Guid IID_IDXGISwapChain3 = new("94d99bdb-f1f8-4ab0-b236-7da0170edab1");
    private static readonly Guid IID_ISwapChainPanelNative = new("63aad0b8-7c24-40ff-85a8-640d944cc325");

    private readonly SwapChainPanel _panel;
    private readonly Func<double> _getScaling;
    private readonly object _lock = new();
    private IntPtr _swapChain3;
    private double _appliedScaling;

    public SwapChainPanelHost(SwapChainPanel panel, Func<PixelSize> getSize, Func<double> getScaling)
    {
        _panel = panel;
        _getScaling = getScaling;
        WindowInfo = new SurfaceInfo(getSize, getScaling);
    }

    public EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo WindowInfo { get; }

    public IDisposable BeginTransaction()
    {
        Monitor.Enter(_lock);
        return Disposable.Create(() => Monitor.Exit(_lock));
    }

    public void SetSwapchainContent(IUnknown swapchain)
    {
        ReleaseSwapChain3();
        var iid = IID_IDXGISwapChain3;
        Marshal.ThrowExceptionForHR(Marshal.QueryInterface(swapchain.GetNativeIntPtr(), in iid, out _swapChain3));
        _appliedScaling = 0;

        // ISwapChainPanelNative.SetSwapChain must be called on the UI thread
        var reference = swapchain.CloneReference();
        if (!_panel.DispatcherQueue.TryEnqueue(() =>
            {
                using (reference)
                    SetPanelSwapChain(reference.GetNativeIntPtr());
            }))
            reference.Dispose();
    }

    private void SetPanelSwapChain(IntPtr swapChain)
    {
        var panelUnknown = Marshal.GetIUnknownForObject(_panel);
        try
        {
            var iid = IID_ISwapChainPanelNative;
            Marshal.ThrowExceptionForHR(Marshal.QueryInterface(panelUnknown, in iid, out var native));
            try
            {
                var setSwapChain = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int>)(*(IntPtr**)native)[3];
                Marshal.ThrowExceptionForHR(setSwapChain(native, swapChain));
            }
            finally
            {
                Marshal.Release(native);
            }
        }
        finally
        {
            Marshal.Release(panelUnknown);
        }
    }

    // The panel composes the swapchain in DIPs, so the pixel-sized buffer needs an inverse scale transform
    public void ResizeIfNeeded(PixelSize size)
    {
        var scaling = _getScaling();
        if (_swapChain3 == IntPtr.Zero || scaling <= 0 || scaling == _appliedScaling)
            return;

        var transform = new DXGI_MATRIX_3X2_F
        {
            _11 = 1.0f / (float)scaling,
            _22 = 1.0f / (float)scaling
        };

        // IDXGISwapChain2::SetMatrixTransform vtable slot:
        // IUnknown(3) + IDXGIObject(4) + IDXGIDeviceSubObject(1) + IDXGISwapChain(10) + IDXGISwapChain1(11)
        // + SetSourceSize, GetSourceSize, SetMaximumFrameLatency, GetMaximumFrameLatency,
        // GetFrameLatencyWaitableObject (5) = 34
        var setMatrixTransform = (delegate* unmanaged[Stdcall]<IntPtr, DXGI_MATRIX_3X2_F*, int>)(*(IntPtr**)_swapChain3)[34];
        Marshal.ThrowExceptionForHR(setMatrixTransform(_swapChain3, &transform));
        _appliedScaling = scaling;
    }

    // No composition effects for embedded content
    public void ApplyEffects(CompositionTransparencyLevel transparencyLevel, PlatformThemeVariant themeVariant)
    {
    }

    private void ReleaseSwapChain3()
    {
        if (_swapChain3 != IntPtr.Zero)
        {
            Marshal.Release(_swapChain3);
            _swapChain3 = IntPtr.Zero;
        }
    }

    public void Dispose()
    {
        lock (_lock)
            ReleaseSwapChain3();
    }

    private sealed class SurfaceInfo : EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo
    {
        private readonly Func<PixelSize> _getSize;
        private readonly Func<double> _getScaling;

        public SurfaceInfo(Func<PixelSize> getSize, Func<double> getScaling)
        {
            _getSize = getSize;
            _getScaling = getScaling;
        }

        public IntPtr Handle => IntPtr.Zero;
        public PixelSize Size => _getSize();
        public double Scaling => _getScaling();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DXGI_MATRIX_3X2_F
    {
        public float _11, _12;
        public float _21, _22;
        public float _31, _32;
    }
}
