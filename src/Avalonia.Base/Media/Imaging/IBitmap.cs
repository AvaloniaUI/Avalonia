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
        /// Saves the bitmap to a file.
        /// </summary>
        /// <param name="fileName">The filename.</param>
        /// <param name="quality">
        /// The optional quality for compression if supported by the specific backend. 
        /// The quality value is interpreted from 0 - 100. If quality is null the default quality 
        /// setting of the backend is applied.
        /// </param>
        void Save(string fileName, int? quality = null);

        /// <summary>
        /// Saves the bitmap to a stream in png format.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="quality">
        /// The optional quality for compression if supported by the specific backend. 
        /// The quality value is interpreted from 0 - 100. If quality is null the default quality 
        /// setting of the backend is applied.
        /// </param>
        void Save(Stream stream, int? quality = null);
    }
}
