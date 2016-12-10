// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Direct2D1.Media;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Win32.Interop;
using SharpDX;
using SharpDX.Direct2D1;

namespace Avalonia.Direct2D1
{
    public class HwndRenderTarget : IRenderTarget
    {
        private readonly IntPtr _hwnd;
        private readonly SharpDX.Direct3D11.Device _d3dDevice;
        private DeviceContext _deviceContext;
        private SharpDX.DXGI.SwapChain1 _swapChain;
        private Size2 _savedSize;
        private Size2F _savedDpi;

        /// <summary>
        /// Initializes a new instance of the <see cref="HwndRenderTarget"/> class.
        /// </summary>
        /// <param name="hwnd">The window handle.</param>
        public HwndRenderTarget(IntPtr hwnd)
        {
            _hwnd = hwnd;
            Direct2DFactory = AvaloniaLocator.Current.GetService<Factory>();
            DirectWriteFactory = AvaloniaLocator.Current.GetService<SharpDX.DirectWrite.Factory>();

            var featureLevels = new[]
            {
                SharpDX.Direct3D.FeatureLevel.Level_12_1,
                SharpDX.Direct3D.FeatureLevel.Level_12_0,
                SharpDX.Direct3D.FeatureLevel.Level_11_1,
                SharpDX.Direct3D.FeatureLevel.Level_11_0,
                SharpDX.Direct3D.FeatureLevel.Level_10_1,
                SharpDX.Direct3D.FeatureLevel.Level_10_0,
                SharpDX.Direct3D.FeatureLevel.Level_9_3,
                SharpDX.Direct3D.FeatureLevel.Level_9_2,
                SharpDX.Direct3D.FeatureLevel.Level_9_1,
            };

            _d3dDevice = new SharpDX.Direct3D11.Device(
                SharpDX.Direct3D.DriverType.Hardware,
                SharpDX.Direct3D11.DeviceCreationFlags.BgraSupport,
                featureLevels);

            CreateSwapChain();
        }

        /// <summary>
        /// Gets the Direct2D factory.
        /// </summary>
        public Factory Direct2DFactory
        {
            get;
        }

        /// <summary>
        /// Gets the DirectWrite factory.
        /// </summary>
        public SharpDX.DirectWrite.Factory DirectWriteFactory
        {
            get;
        }

        /// <summary>
        /// Creates a drawing context for a rendering session.
        /// </summary>
        /// <returns>An <see cref="Avalonia.Media.DrawingContext"/>.</returns>
        public IDrawingContextImpl CreateDrawingContext()
        {
            var size = GetWindowSize();
            var dpi = GetWindowDpi();

            if (size != _savedSize || dpi != _savedDpi)
            {
                _savedSize = size;
                _savedDpi = dpi;
                CreateSwapChain();
            }

            return new DrawingContextImpl(_deviceContext, DirectWriteFactory, _swapChain);
        }

        public void Dispose()
        {
            _deviceContext.Dispose();
            _swapChain.Dispose();
            _d3dDevice.Dispose();
        }

        private void CreateSwapChain()
        {
            using (var dxgiDevice = _d3dDevice.QueryInterface<SharpDX.DXGI.Device>())
            using (var d2dDevice = new Device(dxgiDevice))
            using (var dxgiAdaptor = dxgiDevice.Adapter)
            using (var dxgiFactory = dxgiAdaptor.GetParent<SharpDX.DXGI.Factory2>())
            {
                _deviceContext?.Dispose();
                _deviceContext = new DeviceContext(d2dDevice, DeviceContextOptions.None);

                var swapChainDesc = new SharpDX.DXGI.SwapChainDescription1
                {
                    Width = 0,
                    Height = 0,
                    Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                    Stereo = false,
                    SampleDescription = new SharpDX.DXGI.SampleDescription
                    {
                        Count = 1,
                        Quality = 0,
                    },
                    Usage = SharpDX.DXGI.Usage.RenderTargetOutput,
                    BufferCount = 2,
                    Scaling = SharpDX.DXGI.Scaling.None,
                    SwapEffect = SharpDX.DXGI.SwapEffect.FlipSequential,
                    Flags = 0,
                };

                var dpi = Direct2DFactory.DesktopDpi;

                _swapChain?.Dispose();
                _swapChain = new SharpDX.DXGI.SwapChain1(dxgiFactory, _d3dDevice, _hwnd, ref swapChainDesc);

                using (var dxgiBackBuffer = _swapChain.GetBackBuffer<SharpDX.DXGI.Surface>(0))
                using (var d2dBackBuffer = new Bitmap1(
                    _deviceContext,
                    dxgiBackBuffer,
                    new BitmapProperties1(
                        new PixelFormat
                        {
                            AlphaMode = AlphaMode.Ignore,
                            Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm
                        },
                        dpi.Width,
                        dpi.Height,
                        BitmapOptions.Target | BitmapOptions.CannotDraw)))
                {
                    _deviceContext.Target = d2dBackBuffer;
                }
            }
        }

        private Size2F GetWindowDpi()
        {
            if (UnmanagedMethods.ShCoreAvailable)
            {
                uint dpix, dpiy;

                var monitor = UnmanagedMethods.MonitorFromWindow(
                    _hwnd,
                    UnmanagedMethods.MONITOR.MONITOR_DEFAULTTONEAREST);

                if (UnmanagedMethods.GetDpiForMonitor(
                        monitor,
                        UnmanagedMethods.MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI,
                        out dpix,
                        out dpiy) == 0)
                {
                    return new Size2F(dpix, dpiy);
                }
            }

            return new Size2F(96, 96);
        }

        private Size2 GetWindowSize()
        {
            UnmanagedMethods.RECT rc;
            UnmanagedMethods.GetClientRect(_hwnd, out rc);
            return new Size2(rc.right - rc.left, rc.bottom - rc.top);
        }
    }
}
