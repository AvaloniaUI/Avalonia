using Avalonia.Media.Immutable;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Unique text formatting properties that effect the styling of a text.
    /// </summary>
    public readonly struct TextStyle
    {
        public TextStyle(Typeface typeface, double fontRenderingEmSize = 12, IBrush foreground = null,
            ImmutableTextDecoration[] textDecorations = null)
            : this(new TextFormat(typeface, fontRenderingEmSize), foreground, textDecorations)
        {
        }

        public TextStyle(TextFormat textFormat, IBrush foreground = null,
            ImmutableTextDecoration[] textDecorations = null)
        {
            TextFormat = textFormat;
            Foreground = foreground;
            TextDecorations = textDecorations;
        }

        /// <summary>
        /// Gets the text format.
        /// </summary>
        public TextFormat TextFormat { get; }

        /// <summary>
        /// Gets the foreground.
        /// </summary>
        public IBrush Foreground { get; }

        /// <summary>
        /// Gets the text decorations.
        /// </summary>
        public ImmutableTextDecoration[] TextDecorations { get; }
    }
}
