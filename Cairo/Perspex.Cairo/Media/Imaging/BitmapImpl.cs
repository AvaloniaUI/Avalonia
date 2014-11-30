// -----------------------------------------------------------------------
// <copyright file="BitmapImpl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Cairo.Media.Imaging
{
    using System;
    using global::Cairo;
    using Perspex.Platform;

    public class BitmapImpl : IBitmapImpl
    {
        private ImageSurface surface;

        public BitmapImpl(ImageSurface surface)
        {
            this.surface = surface;
        }

        public int PixelWidth
        {
            get { return this.surface.Width; }
        }

        public int PixelHeight
        {
            get { return this.surface.Height; }
        }

        public void Save(string fileName)
        {
            throw new NotImplementedException();
        }
    }
}
