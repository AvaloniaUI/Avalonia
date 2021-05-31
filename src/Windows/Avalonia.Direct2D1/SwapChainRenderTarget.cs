﻿using System.Drawing;
using System.Numerics;
using Avalonia.Direct2D1.Media;
using Avalonia.Direct2D1.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering;
using Vortice.Direct2D1;
using Vortice.DXGI;

namespace Avalonia.Direct2D1
{   
    public abstract class SwapChainRenderTarget : IRenderTarget, ILayerFactory
    {
        private System.Drawing.Size _savedSize;
        private SizeF _savedDpi;
        private ID2D1DeviceContext _deviceContext;
        private IDXGISwapChain1 _swapChain;

        /// <summary>
        /// Creates a drawing context for a rendering session.
        /// </summary>
        /// <returns>An <see cref="Avalonia.Platform.IDrawingContextImpl"/>.</returns>
        public IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
        {
            var size = GetWindowSize();
            var dpi = GetWindowDpi();

            if (size != _savedSize || dpi != _savedDpi)
            {
                _savedSize = size;
                _savedDpi = dpi;

                Resize();
            }

            return new DrawingContextImpl(visualBrushRenderer, this, _deviceContext, _swapChain);
        }

        public IDrawingContextLayerImpl CreateLayer(Size size)
        {
            if (_deviceContext == null)
            {
                CreateDeviceContext();
            }

            return D2DRenderTargetBitmapImpl.CreateCompatible(_deviceContext, size);
        }

        public void Dispose()
        {
            _deviceContext?.Dispose();

            _swapChain?.Dispose();
        }

        private void Resize()
        {
            _deviceContext?.Dispose();
            _deviceContext = null;

            _swapChain?.ResizeBuffers(0, 0, 0, Format.Unknown, SwapChainFlags.None);

            CreateDeviceContext();
        }

        private void CreateSwapChain()
        {
            var swapChainDescription = new SwapChainDescription1
            {
                Width = _savedSize.Width,
                Height = _savedSize.Height,
                Format = Format.B8G8R8A8_UNorm,
                SampleDescription = new SampleDescription
                {
                    Count = 1,
                    Quality = 0,
                },
                Usage = Usage.RenderTargetOutput,
                BufferCount = 1,
                SwapEffect = SwapEffect.Discard,
            };

            using (var dxgiAdapter = Direct2D1Platform.DxgiDevice.GetAdapter())
            using (var dxgiFactory = dxgiAdapter.GetParent<IDXGIFactory2>())
            {
                _swapChain = CreateSwapChain(dxgiFactory, swapChainDescription);
            }
        }

        private void CreateDeviceContext()
        {
            _deviceContext = Direct2D1Platform.Direct2D1Device.CreateDeviceContext(DeviceContextOptions.None);
            _deviceContext.Dpi = _savedDpi;

            if (_swapChain == null)
            {
                CreateSwapChain();
            }

            using (var dxgiBackBuffer = _swapChain.GetBuffer<IDXGISurface>(0))
            using (var d2dBackBuffer = _deviceContext.CreateBitmapFromDxgiSurface(
                dxgiBackBuffer,
                new BitmapProperties1(
                    new Vortice.DCommon.PixelFormat
                    {
                        AlphaMode = Vortice.DCommon.AlphaMode.Premultiplied,
                        Format = Format.B8G8R8A8_UNorm
                    },
                    _savedSize.Width,
                    _savedSize.Height,
                    BitmapOptions.Target | BitmapOptions.CannotDraw)))
            {
                _deviceContext.Target = d2dBackBuffer;
            }
        }

        protected abstract IDXGISwapChain1 CreateSwapChain(IDXGIFactory2 dxgiFactory, SwapChainDescription1 swapChainDesc);

        protected abstract System.Drawing.SizeF GetWindowDpi();

        protected abstract System.Drawing.Size GetWindowSize();
    }
}
