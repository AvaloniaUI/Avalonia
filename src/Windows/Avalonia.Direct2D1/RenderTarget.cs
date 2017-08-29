// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Direct2D1.Media;
using Avalonia.Direct2D1.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering;
using SharpDX.Direct2D1;
using DwFactory = SharpDX.DirectWrite.Factory;
using WicFactory = SharpDX.WIC.ImagingFactory;

namespace Avalonia.Direct2D1
{
    public class RenderTarget : IRenderTarget, ILayerFactory
    {
        /// <summary>
        /// The render target.
        /// </summary>
        private readonly SharpDX.Direct2D1.RenderTarget _renderTarget;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderTarget"/> class.
        /// </summary>
        /// <param name="renderTarget">The render target.</param>
        public RenderTarget(SharpDX.Direct2D1.RenderTarget renderTarget)
        {
            Direct2DFactory = AvaloniaLocator.Current.GetService<Factory>();
            DirectWriteFactory = AvaloniaLocator.Current.GetService<DwFactory>();
            WicFactory = AvaloniaLocator.Current.GetService<WicFactory>();
            _renderTarget = renderTarget;
        }

        public Factory Direct2DFactory { get; }
        public DwFactory DirectWriteFactory { get; }
        public WicFactory WicFactory { get; }

        /// <summary>
        /// Creates a drawing context for a rendering session.
        /// </summary>
        /// <returns>An <see cref="Avalonia.Platform.IDrawingContextImpl"/>.</returns>
        public IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
        {
            return new DrawingContextImpl(visualBrushRenderer, this, _renderTarget, DirectWriteFactory, WicFactory);
        }

        public IRenderTargetBitmapImpl CreateLayer(Size size)
        {
            return D2DRenderTargetBitmapImpl.CreateCompatible(
                WicFactory,
                DirectWriteFactory,
                _renderTarget,
                size);
        }

        public void Dispose()
        {
            _renderTarget.Dispose();
        }
    }
}
