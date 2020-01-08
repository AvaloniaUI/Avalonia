namespace Avalonia.Media.Text
{
    public readonly struct TextParagraphProperties
    {
        public TextParagraphProperties(
            TextRunStyle defaultTextRunStyle,
            TextAlignment textAlignment = TextAlignment.Left,
            TextWrapping textWrapping = TextWrapping.NoWrap,
            TextTrimming textTrimming = TextTrimming.None)
        {
            DefaultTextRunStyle = defaultTextRunStyle;
            TextAlignment = textAlignment;
            TextWrapping = textWrapping;
            TextTrimming = textTrimming;
        }

        public TextRunStyle DefaultTextRunStyle { get; }

        public TextAlignment TextAlignment { get; }

        public TextWrapping TextWrapping { get; }

        public TextTrimming TextTrimming { get; }
    }
}
