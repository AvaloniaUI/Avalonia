// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Utilities;
using Avalonia.Visuals.Media.Imaging;

namespace Avalonia.Platform
{
    /// <summary>
    /// Defines the interface through which drawing occurs.
    /// </summary>
    public interface IDrawingContextImpl : IDisposable
    {
        /// <summary>
        /// Gets or sets the current transform of the drawing context.
        /// </summary>
        Matrix Transform { get; set; }

        /// <summary>
        /// Clears the render target to the specified color.
        /// </summary>
        /// <param name="color">The color.</param>
        void Clear(Color color);

        /// <summary>
        /// Draws a bitmap image.
        /// </summary>
        /// <param name="source">The bitmap image.</param>
        /// <param name="opacity">The opacity to draw with.</param>
        /// <param name="sourceRect">The rect in the image to draw.</param>
        /// <param name="destRect">The rect in the output to draw to.</param>
        /// <param name="bitmapInterpolationMode">The bitmap interpolation mode.</param>
        void DrawBitmap(IRef<IBitmapImpl> source, double opacity, Rect sourceRect, Rect destRect, BitmapInterpolationMode bitmapInterpolationMode = BitmapInterpolationMode.Default);

        /// <summary>
        /// Draws a bitmap image.
        /// </summary>
        /// <param name="source">The bitmap image.</param>
        /// <param name="opacityMask">The opacity mask to draw with.</param>
        /// <param name="opacityMaskRect">The destination rect for the opacity mask.</param>
        /// <param name="destRect">The rect in the output to draw to.</param>
        void DrawBitmap(IRef<IBitmapImpl> source, IBrush opacityMask, Rect opacityMaskRect, Rect destRect);

        /// <summary>
        /// Draws a line.
        /// </summary>
        /// <param name="pen">The stroke pen.</param>
        /// <param name="p1">The first point of the line.</param>
        /// <param name="p2">The second point of the line.</param>
        void DrawLine(IPen pen, Point p1, Point p2);

        /// <summary>
        /// Draws a geometry.
        /// </summary>
        /// <param name="brush">The fill brush.</param>
        /// <param name="pen">The stroke pen.</param>
        /// <param name="geometry">The geometry.</param>
        void DrawGeometry(IBrush brush, IPen pen, IGeometryImpl geometry);

        /// <summary>
        /// Draws a rectangle with the specified Brush and Pen.
        /// </summary>
        /// <param name="brush">The brush used to fill the rectangle, or <c>null</c> for no fill.</param>
        /// <param name="pen">The pen used to stroke the rectangle, or <c>null</c> for no stroke.</param>
        /// <param name="rect">The rectangle bounds.</param>
        /// <param name="radiusX">The radius in the X dimension of the rounded corners.
        ///     This value will be clamped to the range of 0 to Width/2
        /// </param>
        /// <param name="radiusY">The radius in the Y dimension of the rounded corners.
        ///     This value will be clamped to the range of 0 to Height/2
        /// </param>
        /// <remarks>
        /// The brush and the pen can both be null. If the brush is null, then no fill is performed.
        /// If the pen is null, then no stoke is performed. If both the pen and the brush are null, then the drawing is not visible.
        /// </remarks>
        void DrawRectangle(IBrush brush, IPen pen, Rect rect, double radiusX = 0, double radiusY = 0);

        /// <summary>
        /// Draws text.
        /// </summary>
        /// <param name="foreground">The foreground brush.</param>
        /// <param name="origin">The upper-left corner of the text.</param>
        /// <param name="text">The text.</param>
        void DrawText(IBrush foreground, Point origin, IFormattedTextImpl text);

        /// <summary>
        /// Draws a glyph run.
        /// </summary>
        /// <param name="foreground">The foreground.</param>
        /// <param name="glyphRun">The glyph run.</param>
        /// <param name="baselineOrigin">The baseline origin of the glyph run.</param>
        void DrawGlyphRun(IBrush foreground, GlyphRun glyphRun, Point baselineOrigin);

        /// <summary>
        /// Creates a new <see cref="IRenderTargetBitmapImpl"/> that can be used as a render layer
        /// for the current render target.
        /// </summary>
        /// <param name="size">The size of the layer in DIPs.</param>
        /// <returns>An <see cref="IRenderTargetBitmapImpl"/></returns>
        /// <remarks>
        /// Depending on the rendering backend used, a layer created via this method may be more
        /// performant than a standard render target bitmap. In particular the Direct2D backend
        /// has to do a format conversion each time a standard render target bitmap is rendered,
        /// but a layer created via this method has no such overhead.
        /// </remarks>
        IRenderTargetBitmapImpl CreateLayer(Size size);

        /// <summary>
        /// Pushes a clip rectangle.
        /// </summary>
        /// <param name="clip">The clip rectangle.</param>
        void PushClip(Rect clip);

        /// <summary>
        /// Pops the latest pushed clip rectangle.
        /// </summary>
        void PopClip();

        /// <summary>
        /// Pushes an opacity value.
        /// </summary>
        /// <param name="opacity">The opacity.</param>
        void PushOpacity(double opacity);

        /// <summary>
        /// Pops the latest pushed opacity value.
        /// </summary>
        void PopOpacity();

        /// <summary>
        /// Pushes an opacity mask
        /// </summary>
        void PushOpacityMask(IBrush mask, Rect bounds);

        /// <summary>
        /// Pops the latest pushed opacity mask.
        /// </summary>
        void PopOpacityMask();

        /// <summary>
        /// Pushes a clip geometry.
        /// </summary>
        /// <param name="clip">The clip geometry.</param>
        void PushGeometryClip(IGeometryImpl clip);

        /// <summary>
        /// Pops the latest pushed geometry clip.
        /// </summary>
        void PopGeometryClip();

        /// <summary>
        /// Adds a custom draw operation
        /// </summary>
        /// <param name="custom">Custom draw operation</param>
        void Custom(ICustomDrawOperation custom);
    }
}
