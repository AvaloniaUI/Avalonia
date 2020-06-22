namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Represents a text run's style and is used during the layout process of the <see cref="TextFormatter"/>.
    /// </summary>
    public readonly struct TextStyleRun
    {
        public TextStyleRun(TextPointer textPointer, TextStyle style)
        {
            TextPointer = textPointer;
            Style = style;
        }

        /// <summary>
        /// Gets the text pointer.
        /// </summary>
        public TextPointer TextPointer { get; }

        /// <summary>
        /// Gets the text style.
        /// </summary>
        public TextStyle Style { get; }
    }
}
