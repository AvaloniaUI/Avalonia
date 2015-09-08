





namespace Perspex.Media.Imaging
{
    using Perspex.Platform;

    /// <summary>
    /// Represents a bitmap image.
    /// </summary>
    public interface IBitmap
    {
        /// <summary>
        /// Gets the width of the bitmap, in pixels.
        /// </summary>
        int PixelWidth { get; }

        /// <summary>
        /// Gets the height of the bitmap, in pixels.
        /// </summary>
        int PixelHeight { get; }

        /// <summary>
        /// Gets the platform-specific bitmap implementation.
        /// </summary>
        IBitmapImpl PlatformImpl { get; }

        /// <summary>
        /// Saves the bitmap to a file.
        /// </summary>
        /// <param name="fileName">The filename.</param>
        void Save(string fileName);
    }
}
