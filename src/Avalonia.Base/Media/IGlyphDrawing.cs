namespace Avalonia.Media
{
    /// <summary>
    /// Represents a drawable glyph with bounds information.
    /// </summary>
    public interface IGlyphDrawing
    {
        /// <summary>
        /// Gets the bounds of the glyph drawing.
        /// </summary>
        Rect Bounds { get; }

        /// <summary>
        /// Draws the glyph using the specified drawing context and origin.
        /// </summary>
        /// <param name="context">The drawing context to render to.</param>
        /// <param name="origin">The origin point at which to draw the glyph.</param>
        void Draw(DrawingContext context, Point origin);
    }
}
