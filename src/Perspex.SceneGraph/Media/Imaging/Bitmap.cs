





namespace Perspex.Media.Imaging
{
    using Perspex.Platform;
    using Splat;

    /// <summary>
    /// Holds a bitmap image.
    /// </summary>
    public class Bitmap : IBitmap
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Bitmap"/> class.
        /// </summary>
        /// <param name="fileName">The filename of the bitmap.</param>
        public Bitmap(string fileName)
        {
            IPlatformRenderInterface factory = Locator.Current.GetService<IPlatformRenderInterface>();
            this.PlatformImpl = factory.LoadBitmap(fileName);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Bitmap"/> class.
        /// </summary>
        /// <param name="width">The width of the bitmap, in pixels.</param>
        /// <param name="height">The height of the bitmap, in pixels.</param>
        public Bitmap(int width, int height)
        {
            IPlatformRenderInterface factory = Locator.Current.GetService<IPlatformRenderInterface>();
            this.PlatformImpl = factory.CreateBitmap(width, height);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Bitmap"/> class.
        /// </summary>
        /// <param name="impl">A platform-specific bitmap implementation.</param>
        protected Bitmap(IBitmapImpl impl)
        {
            this.PlatformImpl = impl;
        }

        /// <summary>
        /// Gets the width of the bitmap, in pixels.
        /// </summary>
        public int PixelWidth
        {
            get { return this.PlatformImpl.PixelWidth; }
        }

        /// <summary>
        /// Gets the height of the bitmap, in pixels.
        /// </summary>
        public int PixelHeight
        {
            get { return this.PlatformImpl.PixelHeight; }
        }

        /// <summary>
        /// Gets the platform-specific bitmap implementation.
        /// </summary>
        public IBitmapImpl PlatformImpl
        {
            get;
            private set;
        }

        /// <summary>
        /// Saves the bitmap to a file.
        /// </summary>
        /// <param name="fileName">The filename.</param>
        public void Save(string fileName)
        {
            this.PlatformImpl.Save(fileName);
        }
    }
}
