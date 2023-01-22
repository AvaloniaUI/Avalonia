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
        /// Gets the starting code unit offset of this grapheme inside its containing text.
        /// </summary>
        public int Offset { get; }

        /// <summary>
        /// Gets the length of this grapheme, in code units.
        /// </summary>
        public int Length { get; }
    }
}
