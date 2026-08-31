using System;
using System.IO;
using System.IO.Compression;
using Avalonia.Media.Imaging;
using SkiaSharp;

namespace Avalonia.Skia.Helpers
{
    /// <summary>
    /// Helps with saving images to stream/file.
    /// </summary>
    public static class ImageSavingHelper
    {
        /// <summary>
        /// Saves a Skia image to a file in the PNG format.
        /// </summary>
        /// <param name="image">The image to save</param>
        /// <param name="fileName">The output file to save the image to.</param>
        /// <param name="quality">
        /// The optional quality for compression.
        /// The quality value is interpreted from 0 to 100. When null, 100 is used.
        /// </param>
        [Obsolete($"Use the overload accepting {nameof(BitmapEncoderOptions)} instead.")]
        public static void SaveImage(SKImage image, string fileName, int? quality = null)
        {
            SaveImage(image, fileName, PngBitmapEncoderOptions.Default);
        }

        /// <summary>
        /// Saves a Skia image to a file in the specified format.
        /// </summary>
        /// <param name="image">The image to save</param>
        /// <param name="fileName">The output file to save the image to.</param>
        /// <param name="options">
        /// The options specifying the format and settings to use.
        /// Typical usages include <see cref="PngBitmapEncoderOptions"/> and <see cref="JpegBitmapEncoderOptions"/>.
        /// </param>
        public static void SaveImage(SKImage image, string fileName, BitmapEncoderOptions options)
        {
            ArgumentNullException.ThrowIfNull(image);
            ArgumentNullException.ThrowIfNull(fileName);

            using var stream = File.Create(fileName);

            SaveImage(image, stream, options);
        }

        /// <summary>
        /// Saves a Skia image to a stream in the PNG format.
        /// </summary>
        /// <param name="image">The image to save</param>
        /// <param name="stream">The output stream to save the image to.</param>
        /// <param name="quality">
        /// The optional quality for compression.
        /// The quality value is interpreted from 0 to 100. When null, 100 is used.
        /// </param>
        [Obsolete($"Use the overload accepting {nameof(BitmapEncoderOptions)} instead.")]
        public static void SaveImage(SKImage image, Stream stream, int? quality = null)
        {
            SaveImage(image, stream, PngBitmapEncoderOptions.Default);
        }

        /// <summary>
        /// Saves a Skia image to a stream in the specified format.
        /// </summary>
        /// <param name="image">The image to save</param>
        /// <param name="stream">The output stream to save the image to.</param>
        /// <param name="options">
        /// The options specifying the format and settings to use.
        /// Typical usages include <see cref="PngBitmapEncoderOptions"/> and <see cref="JpegBitmapEncoderOptions"/>.
        /// </param>
        public static void SaveImage(SKImage image, Stream stream, BitmapEncoderOptions options)
        {
            ArgumentNullException.ThrowIfNull(image);
            ArgumentNullException.ThrowIfNull(stream);
            ArgumentNullException.ThrowIfNull(options);
            
            var raster = image.ToRasterImage(true);
            
            try
            {
                using var pixmap = raster.PeekPixels() ?? throw new InvalidOperationException("Could not get image pixels");

                using var data = options switch
                {
                    PngBitmapEncoderOptions pngOptions => pixmap.Encode(ToSkia(pngOptions)),
                    JpegBitmapEncoderOptions jpegOptions => pixmap.Encode(ToSkia(jpegOptions)),
                    _ => throw new ArgumentOutOfRangeException(nameof(options), options, "Unknown encoder options type")
                };

                if (data is null)
                    throw new InvalidOperationException("Could not encode image");
                
                data.SaveTo(stream);    
            }
            finally
            {
                if (image != raster)
                    raster.Dispose();
            }
        }

        private static SKPngEncoderOptions ToSkia(PngBitmapEncoderOptions options)
        {
            var zLibLevel = options.CompressionLevel switch
            {
                CompressionLevel.Optimal => 6,
                CompressionLevel.Fastest => 1,
                CompressionLevel.NoCompression => 0,
                CompressionLevel.SmallestSize => 9,
                _ => throw new ArgumentOutOfRangeException(nameof(options), "Unknown compression level")
            };

            return new SKPngEncoderOptions(SKPngEncoderFilterFlags.AllFilters, zLibLevel);
        }

        private static SKJpegEncoderOptions ToSkia(JpegBitmapEncoderOptions options)
        {
            if (options.Quality is < 0 or > 100)
                throw new ArgumentOutOfRangeException(nameof(options), "Unknown quality");
            
            return new SKJpegEncoderOptions(options.Quality);
        }

        // This method is here mostly for debugging purposes
        internal static void SavePicture(SKPicture picture, float scale, string path)
        {
            var snapshotSize = new SKSizeI((int)Math.Ceiling(picture.CullRect.Width * scale),
                (int)Math.Ceiling(picture.CullRect.Height * scale));
            using var snap =
                SKImage.FromPicture(picture, snapshotSize, SKMatrix.CreateScale(scale, scale));
            SaveImage(snap, path, PngBitmapEncoderOptions.Default);
        }
    }
}
