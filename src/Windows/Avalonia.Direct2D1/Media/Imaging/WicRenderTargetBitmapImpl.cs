// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Platform;
using Avalonia.Rendering;
using SharpDX.Direct2D1;

namespace Avalonia.Direct2D1.Media
{
    public class WicRenderTargetBitmapImpl : WicBitmapImpl, IRenderTargetBitmapImpl
    {
        private readonly WicRenderTarget _renderTarget;

        public WicRenderTargetBitmapImpl(
            int width,
            int height,
            double dpiX,
            double dpiY,
            Platform.PixelFormat? pixelFormat = null)
            : base(width, height, pixelFormat)
        {
            var props = new RenderTargetProperties
            {
                DpiX = (float)dpiX,
                DpiY = (float)dpiY,
            };

            _renderTarget = new WicRenderTarget(
                Direct2D1Platform.Direct2D1Factory,
                WicImpl,
                props);
        }

        public override void Dispose()
        {
            _renderTarget.Dispose();

            base.Dispose();
        }

        public virtual IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
            => CreateDrawingContext(visualBrushRenderer, null);

        public IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer, Action finishedCallback)
        {
            return new DrawingContextImpl(visualBrushRenderer, null, _renderTarget, finishedCallback: finishedCallback);
        }
    }
}
