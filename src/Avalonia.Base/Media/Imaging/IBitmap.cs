using System;
using System.IO;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Media.Imaging
{
    /// <summary>
    /// Represents a bitmap image.
    /// </summary>
    [NotClientImplementable]
    internal interface IBitmap : IImage, IDisposable
    {
        /// <summary>
        /// Gets the dots per inch (DPI) of the image.
        /// </summary>
        /// <remarks>
        /// Note that Skia does not currently support reading the DPI of an image so this value
        /// will always be 96dpi on Skia.
        /// </remarks>
        Vector Dpi { get; }

        /// <summary>
        /// Gets the size of the bitmap, in device pixels.
        /// </summary>
        PixelSize PixelSize { get; }

        /// <summary>
        /// Gets the platform-specific bitmap implementation.
        /// </summary>
        IRef<IBitmapImpl> PlatformImpl { get; }

        /// <summary>
        /// Saves the bitmap to a stream with the specified options.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="options">
        /// The options specifying the format and settings to use.
        /// Typical usages include <see cref="PngBitmapEncoderOptions"/> and <see cref="JpegBitmapEncoderOptions"/>.
        /// </param>
        void Save(Stream stream, BitmapEncoderOptions options);
    }
}
