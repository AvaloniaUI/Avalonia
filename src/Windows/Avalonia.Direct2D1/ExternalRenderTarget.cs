using System;
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
        private readonly SharpDX.WIC.ImagingFactory _wicFactory;

        public ExternalRenderTarget(
            IExternalDirect2DRenderTargetSurface externalRenderTargetProvider,
            DirectWriteFactory dwFactory,
            SharpDX.WIC.ImagingFactory wicFactory)
        {
            _externalRenderTargetProvider = externalRenderTargetProvider;
            _dwFactory = dwFactory;
            _wicFactory = wicFactory;
        }

        public void Dispose()
        {
            _externalRenderTargetProvider.DestroyRenderTarget();
        }

        public IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
        {
            var target =  _externalRenderTargetProvider.GetOrCreateRenderTarget();
            _externalRenderTargetProvider.BeforeDrawing();
            return new DrawingContextImpl(visualBrushRenderer, target, _dwFactory, _wicFactory, null, () =>
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
    }
}
