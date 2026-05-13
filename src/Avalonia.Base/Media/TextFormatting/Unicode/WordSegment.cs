namespace Avalonia.Media.TextFormatting.Unicode
{
    /// <summary>
    /// Represents a segment between two Unicode word boundaries.
    /// </summary>
    public readonly ref struct WordSegment
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WordSegment"/> struct.
        /// </summary>
        /// <param name="offset">The segment offset in UTF-16 code units.</param>
        /// <param name="length">The segment length in UTF-16 code units.</param>
        /// <param name="codepointOffset">The segment offset in Unicode code points.</param>
        /// <param name="codepointLength">The segment length in Unicode code points.</param>
        public WordSegment(int offset, int length, int codepointOffset, int codepointLength)
        {
            Offset = offset;
            Length = length;
            CodepointOffset = codepointOffset;
            CodepointLength = codepointLength;
        }

        /// <summary>
        /// Gets the segment offset in UTF-16 code units.
        /// </summary>
        public int Offset { get; }

        /// <summary>
        /// Gets the segment length in UTF-16 code units.
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// Gets the segment offset in Unicode code points.
        /// </summary>
        public int CodepointOffset { get; }

        /// <summary>
        /// Gets the segment length in Unicode code points.
        /// </summary>
        public int CodepointLength { get; }
    }
}
