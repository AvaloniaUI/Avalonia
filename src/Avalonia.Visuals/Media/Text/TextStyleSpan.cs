namespace Avalonia.Media.Text
{
    public readonly struct TextRunProperties
    {
        public TextRunProperties(TextPointer textPointer, TextRunStyle style)
        {
            TextPointer = textPointer;
            Style = style;
        }

        /// <summary>
        ///     The text pointer.
        /// </summary>
        public TextPointer TextPointer { get; }

        /// <summary>
        ///     The text run properties.
        /// </summary>
        public TextRunStyle Style { get; }

        private TextRunProperties WithTextPointer(TextPointer textPointer)
        {
            return new TextRunProperties(textPointer, Style);
        }

        public SplitResult Split(int length)
        {
            var first = TextPointer.Take(length);

            var second = TextPointer.Skip(length);

            return new SplitResult(WithTextPointer(first), WithTextPointer(second));
        }

        public readonly struct SplitResult
        {
            public SplitResult(TextRunProperties first, TextRunProperties second)
            {
                First = first;
                Second = second;
            }

            public TextRunProperties First { get; }

            public TextRunProperties Second { get; }
        }
    }
}
