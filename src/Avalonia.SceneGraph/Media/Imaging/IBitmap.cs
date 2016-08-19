// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.IO;
using Avalonia.Platform;

namespace Avalonia.Media.Imaging
{
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

        /// <summary>
        /// Saves the bitmap to a stream in png format.
        /// </summary>
        /// <param name="stream">The stream.</param>
        void Save(Stream stream);
    }
}
