// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media;

namespace Avalonia.Platform
{
    /// <summary>
    /// Defines the main platform-specific interface for the rendering subsystem.
    /// </summary>
    public interface IPlatformRenderInterface
    {
        /// <summary>
        /// Creates a formatted text implementation.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="fontFamilyName">The font family.</param>
        /// <param name="fontSize">The font size.</param>
        /// <param name="fontStyle">The font style.</param>
        /// <param name="textAlignment">The text alignment.</param>
        /// <param name="fontWeight">The font weight.</param>
        /// <param name="wrapping">The text wrapping mode.</param>
        /// <returns>An <see cref="IFormattedTextImpl"/>.</returns>
        IFormattedTextImpl CreateFormattedText(
            string text,
            string fontFamilyName,
            double fontSize,
            FontStyle fontStyle,
            TextAlignment textAlignment,
            FontWeight fontWeight,
            TextWrapping wrapping);

        /// <summary>
        /// Creates a stream geometry implementation.
        /// </summary>
        /// <returns>An <see cref="IStreamGeometryImpl"/>.</returns>
        IStreamGeometryImpl CreateStreamGeometry();

        /// <summary>
        /// Creates a renderer.
        /// </summary>
        /// <param name="surfaces">
        /// The list of native platform surfaces that can be used for output.
        /// </param>
        /// <returns>An <see cref="IRenderTarget"/>.</returns>
        IRenderTarget CreateRenderTarget(IEnumerable<object> surfaces);

        /// <summary>
        /// Creates a render target bitmap implementation.
        /// </summary>
        /// <param name="width">The width of the bitmap.</param>
        /// <param name="height">The height of the bitmap.</param>
        /// <returns>An <see cref="IRenderTargetBitmapImpl"/>.</returns>
        IRenderTargetBitmapImpl CreateRenderTargetBitmap(int width, int height);

        /// <summary>
        /// Loads a bitmap implementation from a file..
        /// </summary>
        /// <param name="fileName">The filename of the bitmap.</param>
        /// <returns>An <see cref="IBitmapImpl"/>.</returns>
        IBitmapImpl LoadBitmap(string fileName);

        /// <summary>
        /// Loads a bitmap implementation from a file..
        /// </summary>
        /// <param name="stream">The stream to read the bitmap from.</param>
        /// <returns>An <see cref="IBitmapImpl"/>.</returns>
        IBitmapImpl LoadBitmap(Stream stream);

        /// <summary>
        /// Loads a bitmap implementation from a pixels in memory..
        /// </summary>
        /// <param name="format">Pixel format</param>
        /// <param name="data">Pointer to source bytes</param>
        /// <param name="width">Bitmap width</param>
        /// <param name="height">Bitmap height</param>
        /// <param name="stride">Bytes per row</param>
        /// <returns>An <see cref="IBitmapImpl"/>.</returns>
        IBitmapImpl LoadBitmap(PixelFormat format, IntPtr data, int width, int height, int stride);
    }
}
