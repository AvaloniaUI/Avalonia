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
            double dpiY)
            : base(imagingFactory, width, height)
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

        public IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
        {
            return new DrawingContextImpl(visualBrushRenderer, _target, _dwriteFactory, ImagingFactory);
        }

        public IRenderTargetBitmapImpl CreateLayer(int pixelWidth, int pixelHeight)
        {
            var platform = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();
            return platform.CreateRenderTargetBitmap(
                pixelWidth,
                pixelHeight,
                _target.DotsPerInch.Width,
                _target.DotsPerInch.Height);
        }
    }
}
