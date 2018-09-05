using Avalonia.Direct2D1.Media;
using Avalonia.Direct2D1.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering;

using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DXGI;

using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using PixelFormat = SharpDX.Direct2D1.PixelFormat;

namespace Avalonia.Direct2D1
{
    public abstract class SwapChainRenderTarget : IRenderTarget, ILayerFactory
    {
        private Size2 _savedSize;
        private Size2F _savedDpi;
        private DeviceContext _deviceContext;
        private SwapChain1 _swapChain;

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
                CreateSwapChain();
            }

            return new DrawingContextImpl(visualBrushRenderer, this, _deviceContext, _swapChain);
        }

        public IRenderTargetBitmapImpl CreateLayer(Size size)
        {
            if (_deviceContext == null)
            {
                CreateSwapChain();
            }

            return D2DRenderTargetBitmapImpl.CreateCompatible(_deviceContext, size);
        }

        public void Dispose()
        {
            _deviceContext?.Dispose();
            _swapChain?.Dispose();
        }

        private void CreateSwapChain()
        {
            using (var dxgiAdaptor = Direct2D1Platform.DxgiDevice.Adapter)
            using (var dxgiFactory = dxgiAdaptor.GetParent<SharpDX.DXGI.Factory2>())
            {
                _deviceContext?.Dispose();
                _deviceContext = new DeviceContext(Direct2D1Platform.Direct2D1Device, DeviceContextOptions.None) { DotsPerInch = _savedDpi };

                var swapChainDesc = new SwapChainDescription1
                {
                    Width = _savedSize.Width,
                    Height = _savedSize.Height,
                    Format = Format.B8G8R8A8_UNorm,
                    Stereo = false,
                    SampleDescription = new SampleDescription
                    {
                        Count = 1,
                        Quality = 0,
                    },
                    Usage = Usage.RenderTargetOutput,
                    BufferCount = 1,
                    Scaling = Scaling.Stretch,
                    SwapEffect = SwapEffect.Discard,
                    Flags = 0,
                };

                _swapChain?.Dispose();
                _swapChain = CreateSwapChain(dxgiFactory, swapChainDesc);

                using (var dxgiBackBuffer = _swapChain.GetBackBuffer<Surface>(0))
                using (var d2dBackBuffer = new Bitmap1(
                    _deviceContext,
                    dxgiBackBuffer,
                    new BitmapProperties1(
                        new PixelFormat
                        {
                            AlphaMode = AlphaMode.Premultiplied,
                            Format = Format.B8G8R8A8_UNorm
                        },
                        _savedDpi.Width,
                        _savedDpi.Height,
                        BitmapOptions.Target | BitmapOptions.CannotDraw)))
                {
                    _deviceContext.Target = d2dBackBuffer;
                }
            }
        }

        protected abstract SwapChain1 CreateSwapChain(SharpDX.DXGI.Factory2 dxgiFactory, SwapChainDescription1 swapChainDesc);

        protected abstract Size2F GetWindowDpi();

        protected abstract Size2 GetWindowSize();
    }
}
