// -----------------------------------------------------------------------
// <copyright file="BitmapImpl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Direct2D1.Media
{
    using System;
    using System.IO;
    using Perspex.Platform;
    using SharpDX.WIC;

    public class BitmapImpl : IBitmapImpl
    {
        private ImagingFactory factory;

        public BitmapImpl(ImagingFactory factory, int width, int height)
        {
            this.factory = factory;
            this.WicImpl = new Bitmap(
                factory, 
                width, 
                height, 
                PixelFormat.Format32bppPBGRA, 
                BitmapCreateCacheOption.CacheOnLoad);
        }

        public Bitmap WicImpl
        {
            get;
            private set;
        }

        public void Save(string fileName)
        {
            if (Path.GetExtension(fileName) != ".png")
            {
                // Yeah, we need to support other formats.
                throw new NotSupportedException("Use PNG, stoopid.");
            }

            using (FileStream s = new FileStream(fileName, FileMode.Create))
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder(factory);
                encoder.Initialize(s);

                BitmapFrameEncode frame = new BitmapFrameEncode(encoder);
                frame.Initialize();
                frame.WriteSource(this.WicImpl);
                frame.Commit();
                encoder.Commit();
            }
        }
    }
}
