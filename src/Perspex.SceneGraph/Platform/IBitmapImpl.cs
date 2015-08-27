// -----------------------------------------------------------------------
// <copyright file="IBitmapImpl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Platform
{
    /// <summary>
    /// Defines the platform-specific interface for a <see cref="Perspex.Media.Imaging.Bitmap"/>.
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
    }
}
