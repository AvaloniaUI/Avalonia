using System;

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
        /// <remarks>
        /// Segments created through this constructor carry no <see cref="Text"/> slice.
        /// </remarks>
        // TODO13: remove this constructor and the code-unit/code-point readouts; (Offset, Text) is the target shape.
        public WordSegment(int offset, int length, int codepointOffset, int codepointLength)
        {
            Offset = offset;
            Length = length;
            CodepointOffset = codepointOffset;
            CodepointLength = codepointLength;
            Text = default;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WordSegment"/> struct.
        /// </summary>
        /// <param name="offset">The segment start offset in UTF-16 code units within the source span.</param>
        /// <param name="text">The slice of the source span that makes up this segment.</param>
        /// <param name="codepointOffset">The segment offset in Unicode code points.</param>
        /// <param name="codepointLength">The segment length in Unicode code points.</param>
        public WordSegment(int offset, ReadOnlySpan<char> text, int codepointOffset, int codepointLength)
        {
            Offset = offset;
            Length = text.Length;
            CodepointOffset = codepointOffset;
            CodepointLength = codepointLength;
            Text = text;
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

        /// <summary>
        /// Gets the text content of this segment as a slice of the source span.
        /// </summary>
        public ReadOnlySpan<char> Text { get; }
    }
}
