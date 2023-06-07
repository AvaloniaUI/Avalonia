using System;
using System.IO;
using Avalonia.Metadata;

namespace Avalonia.Platform
{
    /// <summary>
    /// Defines the platform-specific interface for a <see cref="Avalonia.Media.Imaging.Bitmap"/>.
    /// </summary>
    [PrivateApi]
    public interface IBitmapImpl : IDisposable
    {
        /// <summary>
        /// Gets the dots per inch (DPI) of the image.
        /// </summary>
        Vector Dpi { get; }

        /// <summary>
        /// Gets the size of the bitmap, in device pixels.
        /// </summary>
        PixelSize PixelSize { get; }
        
        /// <summary>
        /// Version of the pixel data
        /// </summary>
        int Version { get; }

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
