// -----------------------------------------------------------------------
// <copyright file="IDrawingContext.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System;
namespace Perspex.Media
{
    /// <summary>
    /// Defines the interface through which drawing occurs.
    /// </summary>
    public interface IDrawingContext
    {
        /// <summary>
        /// Draws the outline of a rectangle.
        /// </summary>
        /// <param name="pen">The pen.</param>
        /// <param name="rect">The rectangle bounds.</param>
        void DrawRectange(Pen pen, Rect rect);

        /// <summary>
        /// Draws text.
        /// </summary>
        /// <param name="foreground">The foreground brush.</param>
        /// <param name="rect">The bounding rectangle.</param>
        /// <param name="text">The text.</param>
        void DrawText(Brush foreground, Rect rect, FormattedText text);

        /// <summary>
        /// Draws a filled rectangle.
        /// </summary>
        /// <param name="brush">The brush.</param>
        /// <param name="rect">The rectangle bounds.</param>
        void FillRectange(Brush brush, Rect rect);

        /// <summary>
        /// Pushes a matrix transformation.
        /// </summary>
        /// <param name="matrix">The matrix</param>
        /// <returns>A disposable used to undo the transformation.</returns>
        IDisposable PushTransform(Matrix matrix);
    }
}
