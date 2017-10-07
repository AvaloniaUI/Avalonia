// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Platform;
using Avalonia.Rendering;
using SharpDX.Direct2D1;
using SharpDX.WIC;
using DirectWriteFactory = SharpDX.DirectWrite.Factory;

namespace Avalonia.Direct2D1.Media
{
    public class WicRenderTargetBitmapImpl : WicBitmapImpl, IRenderTargetBitmapImpl
    {
        private readonly DirectWriteFactory _dwriteFactory;
        private readonly WicRenderTarget _target;

        public WicRenderTargetBitmapImpl(
            ImagingFactory imagingFactory,
            Factory d2dFactory,
            DirectWriteFactory dwriteFactory,
            int width,
            int height,
            double dpiX,
            double dpiY,
            Platform.PixelFormat? pixelFormat = null)
            : base(imagingFactory, width, height, pixelFormat)
        {
            var props = new RenderTargetProperties
            {
                DpiX = (float)dpiX,
                DpiY = (float)dpiY,
            };

            _target = new WicRenderTarget(
                d2dFactory,
                WicImpl,
                props);

            _dwriteFactory = dwriteFactory;
        }

        public override void Dispose()
        {
            _target.Dispose();
            base.Dispose();
        }

        public virtual IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
            => CreateDrawingContext(visualBrushRenderer, null);

        public IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer, Action finishedCallback)
        {
            return new DrawingContextImpl(visualBrushRenderer, null, _target, _dwriteFactory, WicImagingFactory,
                finishedCallback: finishedCallback);
        }
    }
}
