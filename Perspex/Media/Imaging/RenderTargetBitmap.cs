// -----------------------------------------------------------------------
// <copyright file="RenderTargetBitmap.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media
{
    using Perspex.Platform;
    using Splat;

    public class RenderTargetBitmap : Bitmap
    {
        public RenderTargetBitmap(int width, int height)
            : base(CreateImpl(width, height))
        {
        }

        public new IRenderTargetBitmapImpl PlatformImpl
        {
            get { return (IRenderTargetBitmapImpl)base.PlatformImpl; }
        }

        public void Render(IVisual visual)
        {
            this.PlatformImpl.Render(visual);
        }

        private static IBitmapImpl CreateImpl(int width, int height)
        {
            IPlatformFactory factory = Locator.Current.GetService<IPlatformFactory>();
            return factory.CreateRenderTargetBitmap(width, height);
        }
    }
}
