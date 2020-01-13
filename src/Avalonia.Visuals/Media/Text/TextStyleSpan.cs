namespace Avalonia.Media.Text
{
    public readonly struct TextStyleRun
    {
        public TextStyleRun(TextPointer textPointer, TextStyle style)
        {
            TextPointer = textPointer;
            Style = style;
        }

        /// <summary>
        ///     The text pointer.
        /// </summary>
        public TextPointer TextPointer { get; }

        /// <summary>
        ///     The text style.
        /// </summary>
        public TextStyle Style { get; }

        private TextStyleRun WithTextPointer(TextPointer textPointer)
        {
            return new TextStyleRun(textPointer, Style);
        }

        public SplitResult Split(int length)
        {
            var first = TextPointer.Take(length);

            var second = TextPointer.Skip(length);

            return new SplitResult(WithTextPointer(first), WithTextPointer(second));
        }

        public readonly struct SplitResult
        {
            public SplitResult(TextStyleRun first, TextStyleRun second)
            {
                First = first;
                Second = second;
            }

            public TextStyleRun First { get; }

            public TextStyleRun Second { get; }
        }
    }
}
