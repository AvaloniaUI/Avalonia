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
        /// Get all installed fonts in the system
        /// </summary>
        IEnumerable<string> InstalledFontNames { get; }

        /// <summary>
        /// Creates a formatted text implementation.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="typeface">The base typeface.</param>
        /// <param name="textAlignment">The text alignment.</param>
        /// <param name="wrapping">The text wrapping mode.</param>
        /// <param name="constraint">The text layout constraints.</param>
        /// <param name="spans">The style spans.</param>
        /// <returns>An <see cref="IFormattedTextImpl"/>.</returns>
        IFormattedTextImpl CreateFormattedText(
            string text,
            Typeface typeface,
            TextAlignment textAlignment,
            TextWrapping wrapping,
            Size constraint,
            IReadOnlyList<FormattedTextStyleSpan> spans);

        /// <summary>
        /// Creates an ellipse geometry implementation.
        /// </summary>
        /// <param name="rect">The bounds of the ellipse.</param>
        /// <returns>An ellipse geometry..</returns>
        IGeometryImpl CreateEllipseGeometry(Rect rect);

        /// <summary>
        /// Creates a line geometry implementation.
        /// </summary>
        /// <param name="p1">The start of the line.</param>
        /// <param name="p2">The end of the line.</param>
        /// <returns>A line geometry.</returns>
        IGeometryImpl CreateLineGeometry(Point p1, Point p2);

        /// <summary>
        /// Creates a rectangle geometry implementation.
        /// </summary>
        /// <param name="rect">The bounds of the rectangle.</param>
        /// <returns>A rectangle.</returns>
        IGeometryImpl CreateRectangleGeometry(Rect rect);

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
        /// <param name="size">The size of the bitmap in device pixels.</param>
        /// <param name="dpi">The DPI of the bitmap.</param>
        /// <returns>An <see cref="IRenderTargetBitmapImpl"/>.</returns>
        IRenderTargetBitmapImpl CreateRenderTargetBitmap(PixelSize size, Vector dpi);

        /// <summary>
        /// Creates a writeable bitmap implementation.
        /// </summary>
        /// <param name="size">The size of the bitmap in device pixels.</param>
        /// <param name="dpi">The DPI of the bitmap.</param>
        /// <param name="format">Pixel format (optional).</param>
        /// <returns>An <see cref="IWriteableBitmapImpl"/>.</returns>
        IWriteableBitmapImpl CreateWriteableBitmap(PixelSize size, Vector dpi, PixelFormat? format = null);

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
        /// Loads a bitmap implementation from a pixels in memory.
        /// </summary>
        /// <param name="format">The pixel format.</param>
        /// <param name="data">The pointer to source bytes.</param>
        /// <param name="size">The size of the bitmap in device pixels.</param>
        /// <param name="dpi">The DPI of the bitmap.</param>
        /// <param name="stride">The number of bytes per row.</param>
        /// <returns>An <see cref="IBitmapImpl"/>.</returns>
        IBitmapImpl LoadBitmap(PixelFormat format, IntPtr data, PixelSize size, Vector dpi, int stride);
    }
}
