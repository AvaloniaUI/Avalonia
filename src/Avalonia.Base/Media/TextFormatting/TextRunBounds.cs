namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// The bounding rectangle of text run
    /// </summary>
    public readonly record struct TextRunBounds
    {
        /// <summary>
        /// Constructing TextRunBounds
        /// </summary>
        internal TextRunBounds(Rect bounds, int firstCharacterIndex, int length, TextRun textRun)
        {
            Rectangle = bounds;
            TextSourceCharacterIndex = firstCharacterIndex;
            Length = length;
            TextRun = textRun;
        }

        /// <summary>
        /// First text source character index of text run
        /// </summary>
        public int TextSourceCharacterIndex { get; }

        /// <summary>
        /// character length of bounded text run
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// Text run bounding rectangle
        /// </summary>
        public Rect Rectangle { get; }

        /// <summary>
        /// text run
        /// </summary>
        public TextRun TextRun { get; }
    }
}
