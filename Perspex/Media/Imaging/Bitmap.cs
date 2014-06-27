// -----------------------------------------------------------------------
// <copyright file="Bitmap.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media.Imaging
{
    using Perspex.Platform;
    using Splat;

    public class Bitmap : IBitmap
    {
        public Bitmap(string fileName)
        {
            IPlatformRenderInterface factory = Locator.Current.GetService<IPlatformRenderInterface>();
            this.PlatformImpl = factory.LoadBitmap(fileName);
        }

        public Bitmap(int width, int height)
        {
            IPlatformRenderInterface factory = Locator.Current.GetService<IPlatformRenderInterface>();
            this.PlatformImpl = factory.CreateBitmap(width, height);
        }

        protected Bitmap(IBitmapImpl impl)
        {
            this.PlatformImpl = impl;
        }

        public int PixelWidth
        {
            get { return this.PlatformImpl.PixelWidth; }
        }

        public int PixelHeight
        {
            get { return this.PlatformImpl.PixelHeight; }
        }

        public IBitmapImpl PlatformImpl
        {
            get;
            private set;
        }

        public void Save(string fileName)
        {
            this.PlatformImpl.Save(fileName);
        }
    }
}
