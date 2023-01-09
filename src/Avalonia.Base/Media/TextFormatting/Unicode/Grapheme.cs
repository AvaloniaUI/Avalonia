using System;

namespace Avalonia.Media.TextFormatting.Unicode
{
    /// <summary>
    /// Represents the smallest unit of a writing system of any given language.
    /// </summary>
    public readonly ref struct Grapheme
    {
        public Grapheme(Codepoint firstCodepoint, int offset, int length)
        {
            FirstCodepoint = firstCodepoint;
            Offset = offset;
            Length = length;
        }

        /// <summary>
        /// The first <see cref="Codepoint"/> of the grapheme cluster.
        /// </summary>
        public Codepoint FirstCodepoint { get; }

        /// <summary>
        /// The Offset to the FirstCodepoint
        /// </summary>
        public int Offset { get; }

        /// <summary>
        /// The length of the grapheme cluster
        /// </summary>
        public int Length { get; }
    }
}
