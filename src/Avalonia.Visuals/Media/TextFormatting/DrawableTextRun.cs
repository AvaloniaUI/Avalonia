﻿using Avalonia.Platform;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// A text run that supports drawing content.
    /// </summary>
    public abstract class DrawableTextRun : TextRun
    {
        /// <summary>
        /// Gets the bounds.
        /// </summary>
        public abstract Rect Bounds { get; }

        /// <summary>
        /// Draws the <see cref="DrawableTextRun"/> at the given origin.
        /// </summary>
        /// <param name="drawingContext">The drawing context.</param>
        /// <param name="origin">The origin.</param>
        public abstract void Draw(IDrawingContextImpl drawingContext, Point origin);
    }
}
