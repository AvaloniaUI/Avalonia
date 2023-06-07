namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Represents a metric for a <see cref="TextLine"/> objects,
    /// that holds information about ascent, descent, line gap, size and origin of the text line.
    /// </summary>
    public readonly record struct TextLineMetrics
    {        
        /// <summary>
        /// Gets a value that indicates whether content of the line overflows the specified paragraph width.
        /// </summary>
        public bool HasOverflowed { get; init; }

        /// <summary>
        /// Gets the height of a line of text.
        /// </summary>
        public double Height { get; init; }
        
        /// <summary>
        /// Gets the number of newline characters at the end of a line.
        /// </summary>
        public int NewlineLength { get; init; }
        
        /// <summary>
        /// Gets the distance from the start of a paragraph to the starting point of a line.
        /// </summary>
        public double Start { get; init; }

        /// <summary>
        /// Gets the distance from the top to the baseline of the line of text.
        /// </summary>
        public double TextBaseline { get; init; }

        /// <summary>
        /// Gets the number of whitespace code points beyond the last non-blank character in a line.
        /// </summary>
        public int TrailingWhitespaceLength { get; init; }

        /// <summary>
        /// Gets the width of a line of text, excluding trailing whitespace characters.
        /// </summary>
        public double Width { get; init; }

        /// <summary>
        /// Gets the width of a line of text, including trailing whitespace characters.
        /// </summary>
        public double WidthIncludingTrailingWhitespace { get; init; }

        /// <summary>
        /// Gets the distance from the top-most to bottom-most black pixel in a line.
        /// </summary>
        public double Extent { get; init; }

        /// <summary>
        /// Gets the distance that black pixels extend beyond the bottom alignment edge of a line.
        /// </summary>
        public double OverhangAfter { get; init; }

        /// <summary>
        /// Gets the distance that black pixels extend prior to the left leading alignment edge of the line.
        /// </summary>
        public double OverhangLeading { get; init; }

        /// <summary>
        /// Gets the distance that black pixels extend following the right trailing alignment edge of the line.
        /// </summary>
        public double OverhangTrailing { get; init; }
    }
}
