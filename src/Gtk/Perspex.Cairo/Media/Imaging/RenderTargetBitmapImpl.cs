





namespace Perspex.Cairo.Media.Imaging
{
    using System;
    using Perspex.Platform;
    using Cairo = global::Cairo;

    public class RenderTargetBitmapImpl : BitmapImpl, IRenderTargetBitmapImpl
    {
        public RenderTargetBitmapImpl(
            Cairo.ImageSurface surface)
            : base(surface)
        {
        }

        public void Dispose()
        {
            this.Surface.Dispose();
        }

        public void Render(IVisual visual)
        {
            Renderer renderer = new Renderer(this.Surface);
            renderer.Render(visual, new PlatformHandle(IntPtr.Zero, "RTB"));
        }
    }
}