using System;

namespace Avalonia.Media.TextFormatting.Unicode
{
    /// <summary>
    /// Represents a segment between two Unicode sentence boundaries (UAX #29).
    /// </summary>
    public readonly ref struct SentenceSegment
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SentenceSegment"/> struct.
        /// </summary>
        /// <param name="offset">The segment offset in UTF-16 code units within the source span.</param>
        /// <param name="text">The slice of the source span that makes up this segment.</param>
        public SentenceSegment(int offset, ReadOnlySpan<char> text)
        {
            Offset = offset;
            Text = text;
        }

        /// <summary>
        /// Gets the segment start offset in UTF-16 code units within the source span.
        /// </summary>
        public int Offset { get; }

        /// <summary>
        /// Gets the text content of this segment as a slice of the source span.
        /// </summary>
        public ReadOnlySpan<char> Text { get; }
    }
}
