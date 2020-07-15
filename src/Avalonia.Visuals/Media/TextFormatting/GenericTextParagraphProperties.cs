namespace Avalonia.Media.TextFormatting
{
    public class GenericTextParagraphProperties : TextParagraphProperties
    {
        private TextAlignment _textAlignment;
        private TextWrapping _textWrapping;
        private TextTrimming _textTrimming;
        private double _lineHeight;

        public GenericTextParagraphProperties(
            TextRunProperties defaultTextRunProperties,
            TextAlignment textAlignment = TextAlignment.Left,
            TextWrapping textWrapping = TextWrapping.WrapWithOverflow,
            TextTrimming textTrimming = TextTrimming.None,
            double lineHeight = 0)
        {
            DefaultTextRunProperties = defaultTextRunProperties;

            _textAlignment = textAlignment;

            _textWrapping = textWrapping;

            _textTrimming = textTrimming;

            _lineHeight = lineHeight;
        }

        public override TextRunProperties DefaultTextRunProperties { get; }

        public override TextAlignment TextAlignment => _textAlignment;

        public override TextWrapping TextWrapping => _textWrapping;

        public override TextTrimming TextTrimming => _textTrimming;

        public override double LineHeight => _lineHeight;

        /// <summary>
        /// Set text alignment
        /// </summary>
        internal void SetTextAlignment(TextAlignment textAlignment)
        {
            _textAlignment = textAlignment;
        }

        /// <summary>
        /// Set text wrap
        /// </summary>
        internal void SetTextWrapping(TextWrapping textWrapping)
        {
            _textWrapping = textWrapping;
        }
        /// <summary>
        /// Set text trimming
        /// </summary>
        internal void SetTextTrimming(TextTrimming textTrimming)
        {
            _textTrimming = textTrimming;
        }

        /// <summary>
        /// Set line height
        /// </summary>
        internal void SetLineHeight(double lineHeight)
        {
            _lineHeight = lineHeight;
        }
    }
}
