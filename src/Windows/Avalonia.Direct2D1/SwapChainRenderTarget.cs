using Avalonia.Direct2D1.Media;
using Avalonia.Direct2D1.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DXGI;

namespace Avalonia.Direct2D1
{   
    internal abstract class SwapChainRenderTarget : IRenderTarget, ILayerFactory
    {
        private Size2 _savedSize;
        private Size2F _savedDpi;
        private DeviceContext _deviceContext;
        private SwapChain1 _swapChain;

        /// <summary>
        /// Creates a drawing context for a rendering session.
        /// </summary>
        /// <returns>An <see cref="Avalonia.Platform.IDrawingContextImpl"/>.</returns>
        public IDrawingContextImpl CreateDrawingContext()
        {
            var size = GetWindowSize();
            var dpi = GetWindowDpi();

            if (size != _savedSize || dpi != _savedDpi)
            {
                _savedSize = size;
                _savedDpi = dpi;

                Resize();
            }

            return new DrawingContextImpl(this, _deviceContext, _swapChain);
        }

        public bool IsCorrupted => false;

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

            using (var dxgiAdapter = Direct2D1Platform.DxgiDevice.Adapter)
            using (var dxgiFactory = dxgiAdapter.GetParent<SharpDX.DXGI.Factory2>())
            {
                _swapChain = CreateSwapChain(dxgiFactory, swapChainDescription);
            }
        }

        private void CreateDeviceContext()
        {
            _deviceContext = new DeviceContext(Direct2D1Platform.Direct2D1Device, DeviceContextOptions.None) { DotsPerInch = _savedDpi };

            if (_swapChain == null)
            {
                CreateSwapChain();
            }

            using (var dxgiBackBuffer = _swapChain.GetBackBuffer<Surface>(0))
            using (var d2dBackBuffer = new Bitmap1(
                _deviceContext,
                dxgiBackBuffer,
                new BitmapProperties1(
                    new SharpDX.Direct2D1.PixelFormat
                    {
                        AlphaMode = SharpDX.Direct2D1.AlphaMode.Premultiplied,
                        Format = Format.B8G8R8A8_UNorm
                    },
                    _savedSize.Width,
                    _savedSize.Height,
                    BitmapOptions.Target | BitmapOptions.CannotDraw)))
            {
                _deviceContext.Target = d2dBackBuffer;
            }
        }

        protected abstract SwapChain1 CreateSwapChain(SharpDX.DXGI.Factory2 dxgiFactory, SwapChainDescription1 swapChainDesc);

        protected abstract Size2F GetWindowDpi();

        protected abstract Size2 GetWindowSize();
    }
}
