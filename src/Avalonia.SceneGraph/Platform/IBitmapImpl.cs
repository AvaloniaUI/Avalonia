// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Platform
{
    /// <summary>
    /// Defines the platform-specific interface for a <see cref="Avalonia.Media.Imaging.Bitmap"/>.
    /// </summary>
    public interface IBitmapImpl
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
        /// Saves the bitmap to a file.
        /// </summary>
        /// <param name="fileName">The filename.</param>
        void Save(string fileName);

        /// <summary>
        /// Gets a stream that contains the bitmap data.
        /// </summary>
        /// <returns>A stream that contains the bitmap data.</returns>
        System.IO.Stream GetStream();
    }
}
