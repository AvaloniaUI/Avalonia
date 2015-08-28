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

    /// <summary>
    /// A Direct2D implementation of a <see cref="Perspex.Media.Imaging.Bitmap"/>.
    /// </summary>
    public class BitmapImpl : IBitmapImpl
    {
        private ImagingFactory factory;

        private SharpDX.Direct2D1.Bitmap direct2D;

        /// <summary>
        /// Initializes a new instance of the <see cref="BitmapImpl"/> class.
        /// </summary>
        /// <param name="factory">The WIC imaging factory to use.</param>
        /// <param name="fileName">The filename of the bitmap to load.</param>
        public BitmapImpl(ImagingFactory factory, string fileName)
        {
            this.factory = factory;

            using (BitmapDecoder decoder = new BitmapDecoder(factory, fileName, DecodeOptions.CacheOnDemand))
            {
                this.WicImpl = new Bitmap(factory, decoder.GetFrame(0), BitmapCreateCacheOption.CacheOnDemand);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BitmapImpl"/> class.
        /// </summary>
        /// <param name="factory">The WIC imaging factory to use.</param>
        /// <param name="width">The width of the bitmap.</param>
        /// <param name="height">The height of the bitmap.</param>
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

        /// <summary>
        /// Gets the width of the bitmap, in pixels.
        /// </summary>
        public int PixelWidth
        {
            get { return this.WicImpl.Size.Height; }
        }

        /// <summary>
        /// Gets the height of the bitmap, in pixels.
        /// </summary>
        public int PixelHeight
        {
            get { return this.WicImpl.Size.Width; }
        }

        /// <summary>
        /// Gets the WIC implementation of the bitmap.
        /// </summary>
        public Bitmap WicImpl
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a Direct2D bitmap to use on the specified render target.
        /// </summary>
        /// <param name="renderTarget">The render target.</param>
        /// <returns>The Direct2D bitmap.</returns>
        public SharpDX.Direct2D1.Bitmap GetDirect2DBitmap(SharpDX.Direct2D1.RenderTarget renderTarget)
        {
            if (this.direct2D == null)
            {
                FormatConverter converter = new FormatConverter(this.factory);
                converter.Initialize(this.WicImpl, PixelFormat.Format32bppPBGRA);
                this.direct2D = SharpDX.Direct2D1.Bitmap.FromWicBitmap(renderTarget, converter);
            }

            return this.direct2D;
        }

        /// <summary>
        /// Saves the bitmap to a file.
        /// </summary>
        /// <param name="fileName">The filename.</param>
        public void Save(string fileName)
        {
            if (Path.GetExtension(fileName) != ".png")
            {
                // Yeah, we need to support other formats.
                throw new NotSupportedException("Use PNG, stoopid.");
            }

            using (FileStream s = new FileStream(fileName, FileMode.Create))
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder(this.factory);
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
