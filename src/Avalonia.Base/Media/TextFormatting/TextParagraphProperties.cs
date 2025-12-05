namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Provides a set of properties that are used during the paragraph layout.
    /// </summary>
    public abstract class TextParagraphProperties
    {
        /// <summary>
        /// Gets a value that specifies whether the primary text advance direction shall be left-to-right, or right-to-left.
        /// </summary>
        public abstract FlowDirection FlowDirection { get; }

        /// <summary>
        /// Gets a value that describes how an inline content of a block is aligned.
        /// </summary>
        public abstract TextAlignment TextAlignment { get; }

        /// <summary>
        /// Gets the height of a line of text.
        /// </summary>
        public abstract double LineHeight { get; }

        /// <summary>
        /// Gets or sets paragraph's line spacing.
        /// </summary>
        internal double LineSpacing { get; set; }

        /// <summary>
        /// Gets a value that indicates whether the text run is the first line of the paragraph.
        /// </summary>
        public abstract bool FirstLineInParagraph { get; }

        /// <summary>
        /// Gets a value that indicates whether a formatted line can always be collapsed.
        /// </summary>
        /// <remarks>
        /// If true, the formatted line may always be collapsed. If false (the default),
        /// only lines that overflow the paragraph width are collapsed.
        /// </remarks>
        public virtual bool AlwaysCollapsible
        {
            get { return false; }
        }

        /// <summary>
        /// Gets the default text run properties, such as typeface or foreground brush.
        /// </summary>
        public abstract TextRunProperties DefaultTextRunProperties { get; }

        /// <summary>
        /// Gets the collection of TextDecoration objects.
        /// </summary>
        /// <remarks>
        /// If not null, text decorations to apply to all runs in the line. This is in addition
        /// to any text decorations specified by the TextRunProperties for individual text runs.
        /// </remarks>
        public virtual TextDecorationCollection? TextDecorations => null;

        /// <summary>
        /// Gets a value that controls whether text wraps when it reaches the flow edge of its containing block box.
        /// </summary>
        public abstract TextWrapping TextWrapping { get; }

        /// <summary>
        /// Gets the amount of line indentation.
        /// </summary>
        public abstract double Indent { get; }

        /// <summary>
        /// Gets the paragraph indentation.
        /// </summary>
        public virtual double ParagraphIndent
        {
            get { return 0; }
        }

        /// <summary>
        /// Gets the default incremental tab width.
        /// </summary>
        public virtual double DefaultIncrementalTab => 0;

        /// <summary>
        /// Gets the amount of letter spacing.
        /// </summary>
        public virtual double LetterSpacing { get; }
    }
}
