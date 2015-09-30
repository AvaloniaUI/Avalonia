// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Platform;
using Perspex.Rendering;
using SharpDX.Direct2D1;
using SharpDX.WIC;

namespace Perspex.Direct2D1.Media
{
    public class RenderTargetBitmapImpl : BitmapImpl, IRenderTargetBitmapImpl, IDisposable
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

        public void Dispose()
        {
            // TODO:
        }

        public void Render(IVisual visual)
        {
            RenderTarget renderTarget = new RenderTarget(_target);
            renderTarget.Render(visual, null);
        }
    }
}
