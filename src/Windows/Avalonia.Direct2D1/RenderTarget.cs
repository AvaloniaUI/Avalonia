// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Direct2D1.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
using SharpDX.Direct2D1;
using DwFactory = SharpDX.DirectWrite.Factory;

namespace Avalonia.Direct2D1
{
    public class RenderTarget : IRenderTarget
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
            _renderTarget = renderTarget;
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
        public DwFactory DirectWriteFactory
        {
            get;
        }

        /// <summary>
        /// Creates a drawing context for a rendering session.
        /// </summary>
        /// <returns>An <see cref="Avalonia.Platform.IDrawingContextImpl"/>.</returns>
        public IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
        {
            return new DrawingContextImpl(visualBrushRenderer, _renderTarget, DirectWriteFactory);
        }

        public IRenderTargetBitmapImpl CreateLayer(int pixelWidth, int pixelHeight)
        {
            var platform = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();
            return platform.CreateRenderTargetBitmap(
                pixelWidth,
                pixelHeight,
                _renderTarget.DotsPerInch.Width,
                _renderTarget.DotsPerInch.Height);
        }

        public void Dispose()
        {
            _renderTarget.Dispose();
        }
    }
}
