using System;
using System.IO;
using SkiaSharp;

namespace Avalonia.Skia.Helpers
{
    /// <summary>
    /// Helps with saving images to stream/file.
    /// </summary>
    public static class ImageSavingHelper
    {
        /// <summary>
        /// Save Skia image to a file.
        /// </summary>
        /// <param name="image">Image to save</param>
        /// <param name="fileName">Target file.</param>
        /// <param name="quality">
        /// The optional quality for PNG compression. 
        /// The quality value is interpreted from 0 - 100. If quality is null 
        /// the encoder applies the default quality value.
        /// </param>
        public static void SaveImage(SKImage image, string fileName, int? quality = null)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));
            if (fileName == null) throw new ArgumentNullException(nameof(fileName));

            using (var stream = File.Create(fileName))
            {
                SaveImage(image, stream, quality);
            }
        }

        /// <summary>
        /// Save Skia image to a stream.
        /// </summary>
        /// <param name="image">Image to save</param>
        /// <param name="stream">The where write image.</param>
        /// <param name="quality">
        /// The optional quality for PNG compression. 
        /// The quality value is interpreted from 0 - 100. If quality is null 
        /// the encoder applies the default quality value.
        /// </param>
        public static void SaveImage(SKImage image, Stream stream, int? quality = null)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            if (quality == null)
            {
                using (var data = image.Encode())
                {
                    data.SaveTo(stream);
                }
            }
            else
            {
                using (var data = image.Encode(SKEncodedImageFormat.Png, (int)quality))
                {
                    data.SaveTo(stream);
                }
            }
        }
    }
}
