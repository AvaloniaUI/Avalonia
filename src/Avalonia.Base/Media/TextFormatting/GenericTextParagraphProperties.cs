namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Generic implementation of <see cref="TextParagraphProperties"/>.
    /// </summary>
    public sealed class GenericTextParagraphProperties : TextParagraphProperties
    {
        private FlowDirection _flowDirection;
        private TextAlignment _textAlignment;
        private TextWrapping _textWrap;
        private double _lineHeight;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericTextParagraphProperties"/>.
        /// </summary>
        /// <param name="defaultTextRunProperties">Default text run properties, such as typeface or foreground brush.</param>
        /// <param name="textAlignment">The alignment of inline content in a block.</param>
        /// <param name="textWrapping">A value that controls whether text wraps when it reaches the flow edge of its containing block box.</param>
        /// <param name="lineHeight">Paragraph's line spacing.</param>
        /// <param name="letterSpacing">The amount of letter spacing.</param>
        public GenericTextParagraphProperties(TextRunProperties defaultTextRunProperties,
            TextAlignment textAlignment = TextAlignment.Left,
            TextWrapping textWrapping = TextWrapping.NoWrap,
            double lineHeight = 0,
            double letterSpacing = 0)
        {
            DefaultTextRunProperties = defaultTextRunProperties;
            _textAlignment = textAlignment;
            _textWrap = textWrapping;
            _lineHeight = lineHeight;
            LetterSpacing = letterSpacing;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericTextParagraphProperties"/>.
        /// </summary>
        /// <param name="flowDirection">The primary text advance direction.</param>
        /// <param name="textAlignment">The alignment of inline content in a block.</param>
        /// <param name="firstLineInParagraph"><see langword="true"/> if the paragraph is the first line in the paragraph</param>
        /// <param name="alwaysCollapsible"><see langword="true"/> if the formatted line may always be collapsed. If <see langword="false"/> (the default), only lines that overflow the paragraph width are collapsed.</param>
        /// <param name="defaultTextRunProperties">Default text run properties, such as typeface or foreground brush.</param>
        /// <param name="textWrapping">A value that controls whether text wraps when it reaches the flow edge of its containing block box.</param>
        /// <param name="lineHeight">Paragraph's line spacing.</param>
        /// <param name="indent">The amount of line indentation.</param>
        /// <param name="letterSpacing">The amount of letter spacing.</param>
        public GenericTextParagraphProperties(
            FlowDirection flowDirection,
            TextAlignment textAlignment,
            bool firstLineInParagraph,
            bool alwaysCollapsible,
            TextRunProperties defaultTextRunProperties,
            TextWrapping textWrapping,
            double lineHeight,
            double indent,
            double letterSpacing)
        {
            _flowDirection = flowDirection;
            _textAlignment = textAlignment;
            FirstLineInParagraph = firstLineInParagraph;
            AlwaysCollapsible = alwaysCollapsible;
            DefaultTextRunProperties = defaultTextRunProperties;
            _textWrap = textWrapping;
            _lineHeight = lineHeight;
            LetterSpacing = letterSpacing;
            Indent = indent;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericTextParagraphProperties"/> with values copied from the specified <see cref="TextParagraphProperties"/>.
        /// </summary>
        /// <param name="textParagraphProperties">The <see cref="TextParagraphProperties"/> to copy values from.</param>
        public GenericTextParagraphProperties(TextParagraphProperties textParagraphProperties)
            : this(textParagraphProperties.FlowDirection, 
                textParagraphProperties.TextAlignment,
                textParagraphProperties.FirstLineInParagraph, 
                textParagraphProperties.AlwaysCollapsible,
                textParagraphProperties.DefaultTextRunProperties, 
                textParagraphProperties.TextWrapping,
                textParagraphProperties.LineHeight,
                textParagraphProperties.Indent,
                textParagraphProperties.LetterSpacing)
        {
        }

        /// <inheritdoc/>
        public override FlowDirection FlowDirection
        {
            get { return _flowDirection; }
        }

        /// <inheritdoc/>
        public override TextAlignment TextAlignment
        {
            get { return _textAlignment; }
        }

        /// <inheritdoc/>
        public override double LineHeight
        {
            get { return _lineHeight; }
        }

        /// <inheritdoc/>
        public override bool FirstLineInParagraph { get; }

        /// <inheritdoc/>
        public override bool AlwaysCollapsible { get; }

        /// <inheritdoc/>
        public override TextRunProperties DefaultTextRunProperties { get; }

        /// <inheritdoc/>
        public override TextWrapping TextWrapping
        {
            get { return _textWrap; }
        }

        /// <inheritdoc/>
        public override double Indent { get; }

        /// <inheritdoc/>
        public override double LetterSpacing { get; }

        /// <summary>
        /// Set text flow direction
        /// </summary>
        internal void SetFlowDirection(FlowDirection flowDirection)
        {
            _flowDirection = flowDirection;
        }


        /// <summary>
        /// Set text alignment
        /// </summary>
        internal void SetTextAlignment(TextAlignment textAlignment)
        {
            _textAlignment = textAlignment;
        }


        /// <summary>
        /// Set line height
        /// </summary>
        internal void SetLineHeight(double lineHeight)
        {
            _lineHeight = lineHeight;
        }

        /// <summary>
        /// Set text wrap
        /// </summary>
        internal void SetTextWrapping(TextWrapping textWrap)
        {
            _textWrap = textWrap;
        }
    }
}
