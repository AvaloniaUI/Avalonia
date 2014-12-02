// -----------------------------------------------------------------------
// <copyright file="RenderTargetBitmapImpl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Direct2D1.Media
{
    using System;
    using Perspex.Platform;
    using SharpDX.Direct2D1;
    using SharpDX.WIC;

    public class RenderTargetBitmapImpl : BitmapImpl, IRenderTargetBitmapImpl
    {
        private WicRenderTarget target;

        public RenderTargetBitmapImpl(
            ImagingFactory imagingFactory, 
            Factory d2dFactory,
            int width, 
            int height)
            : base(imagingFactory, width, height)
        {
            this.target = new WicRenderTarget(
                d2dFactory,
                this.WicImpl,
                new RenderTargetProperties
                {
                    DpiX = 96,
                    DpiY = 96,
                });
        }

        public void Render(IVisual visual)
        {
            Renderer renderer = new Renderer(this.target);
            renderer.Render(visual, null);
        }
    }
}
