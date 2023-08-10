using System;
using System.IO;
using System.Threading;

using Avalonia.Logging;

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
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));

            var format = GetFormatFromExtension(Path.GetExtension(fileName));

            using (var stream = File.Create(fileName))
            {
                SaveImage(image, stream, format, quality);
            }
        }

        /// <summary>
        /// Save Skia image to a stream.
        /// </summary>
        /// <param name="image">Image to save</param>
        /// <param name="stream">The output stream to save the image.</param>
        /// <param name="quality">
        /// The optional quality for PNG compression. 
        /// The quality value is interpreted from 0 - 100. If quality is null 
        /// the encoder applies the default quality value.
        /// </param>
        [Obsolete("Use overloads that take format parameter")]
        public static void SaveImage(SKImage image, Stream stream, int? quality = null)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            using var data = image.Encode();
            data.SaveTo(stream);
        }

        /// <summary>
        /// Save Skia image to a stream.
        /// </summary>
        /// <param name="image">Image to save</param>
        /// <param name="stream">The output stream to save the image.</param>
        /// <param name="format">The file format used to encode the image.</param>
        /// <param name="quality">
        /// The optional quality setting for image encoder. 
        /// The quality value is interpreted from 0 - 100. If quality is null 
        /// the encoder applies the default quality value. 
        /// Some formats completely ignore this value.
        /// </param>
        public static void SaveImage(SKImage image, Stream stream, SKEncodedImageFormat format, int? quality = null)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (quality < 0 || quality > 100)
                throw new ArgumentOutOfRangeException(nameof(quality), "[0, 100]");

            using var data = image.Encode(format, quality ?? 100);
            data.SaveTo(stream);
        }

        private static SKEncodedImageFormat GetFormatFromExtension(string extension)
            => extension.ToLowerInvariant() switch
            {
                ".png" => SKEncodedImageFormat.Png,
                ".jpg" or ".jpeg" or ".jpe" => SKEncodedImageFormat.Jpeg,
                ".bmp" => SKEncodedImageFormat.Bmp,
                ".gif" => SKEncodedImageFormat.Gif,
                ".ico" => SKEncodedImageFormat.Ico,
                ".webp" => SKEncodedImageFormat.Webp,
                ".heif" or ".heic" => SKEncodedImageFormat.Heif,
                ".avif" => SKEncodedImageFormat.Avif,
                _ => GetFallbackFormat(),
            };

        private static int s_warnedAboutFallback = 0;
        private static SKEncodedImageFormat GetFallbackFormat()
        {
            if (Interlocked.CompareExchange(ref s_warnedAboutFallback, 1, 0) == 0)
            {
                Logger.TryGet(LogEventLevel.Warning, nameof(Skia))
                    ?.Log(null, "Obsolete behavior: can not deduce image format from extension. Will throw an exception in the future. For now using PNG.");
            }

            return SKEncodedImageFormat.Png;
        }

        // This method is here mostly for debugging purposes
        internal static void SavePicture(SKPicture picture, float scale, string path)
        {
            var snapshotSize = new SKSizeI((int)Math.Ceiling(picture.CullRect.Width * scale),
                (int)Math.Ceiling(picture.CullRect.Height * scale));
            using var snap =
                SKImage.FromPicture(picture, snapshotSize, SKMatrix.CreateScale(scale, scale));
            SaveImage(snap, path);
        }
    }
}
