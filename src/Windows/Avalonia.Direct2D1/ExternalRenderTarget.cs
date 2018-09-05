// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Direct2D1.Media;
using Avalonia.Direct2D1.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering;
using SharpDX;

namespace Avalonia.Direct2D1
{
    using SharpDX.Direct2D1;

    class ExternalRenderTarget : IRenderTarget, ILayerFactory
    {
        private readonly IExternalDirect2DRenderTargetSurface _externalRenderTargetProvider;

        public ExternalRenderTarget(IExternalDirect2DRenderTargetSurface externalRenderTargetProvider)
        {
            _externalRenderTargetProvider = externalRenderTargetProvider;
        }

        public void Dispose()
        {
            _externalRenderTargetProvider.DestroyRenderTarget();
        }

        public IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
        {
            var target =  _externalRenderTargetProvider.GetOrCreateRenderTarget();
            _externalRenderTargetProvider.BeforeDrawing();
            return new DrawingContextImpl(
                visualBrushRenderer, 
                null, 
                target.QueryInterface<DeviceContext>(), 
                null, 
                () =>
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

        public IRenderTargetBitmapImpl CreateLayer(Size size)
        {
            var target = _externalRenderTargetProvider.GetOrCreateRenderTarget();
            return D2DRenderTargetBitmapImpl.CreateCompatible(target, size);
        }
    }
}
