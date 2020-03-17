namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Provides a set of properties that are used during the paragraph layout.
    /// </summary>
    public readonly struct TextParagraphProperties
    {
        public TextParagraphProperties(
            TextStyle defaultTextStyle,
            TextAlignment textAlignment = TextAlignment.Left,
            TextWrapping textWrapping = TextWrapping.NoWrap,
            TextTrimming textTrimming = TextTrimming.None)
        {
            DefaultTextStyle = defaultTextStyle;
            TextAlignment = textAlignment;
            TextWrapping = textWrapping;
            TextTrimming = textTrimming;
        }

        /// <summary>
        /// Gets the default text style.
        /// </summary>
        public TextStyle DefaultTextStyle { get; }

        /// <summary>
        /// Gets the text alignment.
        /// </summary>
        public TextAlignment TextAlignment { get; }

        /// <summary>
        /// Gets the text wrapping.
        /// </summary>
        public TextWrapping TextWrapping { get; }

        /// <summary>
        /// Gets the text trimming.
        /// </summary>
        public TextTrimming TextTrimming { get; }
    }
}
