using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Direct2D1.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
using SharpDX;
using DirectWriteFactory = SharpDX.DirectWrite.Factory;

namespace Avalonia.Direct2D1
{
    class ExternalRenderTarget : IRenderTarget
    {
        private readonly IExternalDirect2DRenderTargetSurface _externalRenderTargetProvider;
        private readonly DirectWriteFactory _dwFactory;
        public ExternalRenderTarget(IExternalDirect2DRenderTargetSurface externalRenderTargetProvider,
            DirectWriteFactory dwFactory)
        {
            _externalRenderTargetProvider = externalRenderTargetProvider;
            _dwFactory = dwFactory;
        }

        public void Dispose()
        {
            _externalRenderTargetProvider.DestroyRenderTarget();
        }

        public IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
        {
            var target =  _externalRenderTargetProvider.GetOrCreateRenderTarget();
            _externalRenderTargetProvider.BeforeDrawing();
            return new DrawingContextImpl(visualBrushRenderer, target, _dwFactory, null, () =>
            {
                try
                {
                    _externalRenderTargetProvider.AfterDrawing();
                }
                catch (SharpDXException ex) when ((uint) ex.HResult == 0x8899000C) // D2DERR_RECREATE_TARGET
                {
                    _externalRenderTargetProvider.DestroyRenderTarget();
                }
            });
        }

        public IRenderTargetBitmapImpl CreateLayer(int pixelWidth, int pixelHeight)
        {
            var platform = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();

            // TODO: Get proper DPI here.
            return platform.CreateRenderTargetBitmap(pixelWidth, pixelHeight, 96, 96);
        }
    }
}
