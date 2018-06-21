// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

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
        public static void SaveImage(SKImage image, string fileName)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));
            if (fileName == null) throw new ArgumentNullException(nameof(fileName));

            using (var stream = File.Create(fileName))
            {
                SaveImage(image, stream);
            }
        }

        /// <summary>
        /// Save Skia image to a stream.
        /// </summary>
        /// <param name="image">Image to save</param>
        /// <param name="stream">Target stream.</param>
        public static void SaveImage(SKImage image, Stream stream)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            using (var data = image.Encode())
            {
                data.SaveTo(stream);
            }
        }
    }
}