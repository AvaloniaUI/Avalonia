// -----------------------------------------------------------------------
// <copyright file="BitmapImpl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Cairo.Media.Imaging
{
    using System;
    using Perspex.Platform;
    using Cairo = global::Cairo;

    public class BitmapImpl : IBitmapImpl
    {
        public BitmapImpl(Cairo.ImageSurface surface)
        {
            this.Surface = surface;
        }

        public int PixelWidth
        {
            get { return this.Surface.Width; }
        }

        public int PixelHeight
        {
            get { return this.Surface.Height; }
        }

        public Cairo.ImageSurface Surface
        {
            get;
            private set;
        }

        public void Save(string fileName)
        {
            throw new NotImplementedException();
        }
    }
}
