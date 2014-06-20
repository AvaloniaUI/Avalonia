// -----------------------------------------------------------------------
// <copyright file="Bitmap.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media
{
    using Perspex.Platform;
    using Splat;

    public class Bitmap
    {
        public Bitmap(int width, int height)
        {
            IPlatformFactory factory = Locator.Current.GetService<IPlatformFactory>();
            this.PlatformImpl = factory.CreateBitmap(width, height);
        }

        protected Bitmap(IBitmapImpl impl)
        {
            this.PlatformImpl = impl;
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
