namespace Avalonia.Media.Text
{
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

        public TextStyle DefaultTextStyle { get; }

        public TextAlignment TextAlignment { get; }

        public TextWrapping TextWrapping { get; }

        public TextTrimming TextTrimming { get; }
    }
}
