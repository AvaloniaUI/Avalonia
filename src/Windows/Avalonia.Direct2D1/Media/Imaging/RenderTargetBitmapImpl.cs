// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
using SharpDX.Direct2D1;
using SharpDX.WIC;

namespace Avalonia.Direct2D1.Media
{
    public class RenderTargetBitmapImpl : WicBitmapImpl, IRenderTargetBitmapImpl
    {
        private readonly WicRenderTarget _target;

        public RenderTargetBitmapImpl(
            ImagingFactory imagingFactory,
            Factory d2dFactory,
            int width,
            int height)
            : base(imagingFactory, width, height)
        {
            var props = new RenderTargetProperties
            {
                DpiX = 96,
                DpiY = 96,
            };

            _target = new WicRenderTarget(
                d2dFactory,
                WicImpl,
                props);
        }

        public override void Dispose()
        {
            _target.Dispose();
            base.Dispose();
        }

        public Avalonia.Media.DrawingContext CreateDrawingContext() => new RenderTarget(_target).CreateDrawingContext();
        
    }
}
