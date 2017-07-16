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
        private SharpDX.Direct2D1.RenderTarget _target;
        public ExternalRenderTarget(IExternalDirect2DRenderTargetSurface externalRenderTargetProvider,
            DirectWriteFactory dwFactory)
        {
            _externalRenderTargetProvider = externalRenderTargetProvider;
            _dwFactory = dwFactory;
        }

        public void Dispose()
        {
            _target?.Dispose();
            _target = null;
        }

        public IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
        {
            _target = _target ?? _externalRenderTargetProvider.CreateRenderTarget();
            _externalRenderTargetProvider.BeforeDrawing();
            return new DrawingContextImpl(visualBrushRenderer, _target, _dwFactory, null, () =>
            {
                try
                {
                    _externalRenderTargetProvider.AfterDrawing();
                }
                catch (SharpDXException ex) when ((uint) ex.HResult == 0x8899000C) // D2DERR_RECREATE_TARGET
                {
                    _target?.Dispose();
                    _target = null;
                }
            });
        }
    }
}
