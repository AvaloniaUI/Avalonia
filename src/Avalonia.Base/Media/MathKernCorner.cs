namespace Avalonia.Media
{
    /// <summary>
    /// A corner of a math glyph's bounding box at which a script attaches and math kerning applies.
    /// </summary>
    public enum MathKernCorner
    {
        /// <summary>
        /// The top right corner, where a superscript attaches.
        /// </summary>
        TopRight,

        /// <summary>
        /// The top left corner, where a prescript superscript attaches.
        /// </summary>
        TopLeft,

        /// <summary>
        /// The bottom right corner, where a subscript attaches.
        /// </summary>
        BottomRight,

        /// <summary>
        /// The bottom left corner, where a prescript subscript attaches.
        /// </summary>
        BottomLeft,
    }
}
