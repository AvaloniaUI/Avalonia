namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Provides a set of properties that are used during the paragraph layout.
    /// </summary>
    public abstract class TextParagraphProperties
    {
        /// <summary>
        /// Gets the text alignment.
        /// </summary>
        public abstract TextAlignment TextAlignment { get; }

        /// <summary>
        /// Gets the default text style.
        /// </summary>
        public abstract TextRunProperties DefaultTextRunProperties { get; }

        /// <summary>
        /// If not null, text decorations to apply to all runs in the line. This is in addition
        /// to any text decorations specified by the TextRunProperties for individual text runs.
        /// </summary>
        public virtual TextDecorationCollection TextDecorations => null;

        /// <summary>
        /// Gets the text wrapping.
        /// </summary>
        public abstract TextWrapping TextWrapping { get; }

        /// <summary>
        /// Paragraph's line height
        /// </summary>
        public abstract double LineHeight { get; }
    }
}
