





namespace Perspex.Direct2D1.Media
{
    using System;
    using Perspex.Platform;
    using SharpDX.Direct2D1;
    using SharpDX.WIC;

    public class RenderTargetBitmapImpl : BitmapImpl, IRenderTargetBitmapImpl, IDisposable
    {
        private WicRenderTarget target;

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

            this.target = new WicRenderTarget(
                d2dFactory,
                this.WicImpl,
                props);
        }

        public void Dispose()
        {
            // TODO:
        }

        public void Render(IVisual visual)
        {
            Renderer renderer = new Renderer(this.target);
            renderer.Render(visual, null);
        }
    }
}
