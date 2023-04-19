namespace Avalonia.Media
{
    public readonly record struct GlyphRunMetrics
    {
        public double Baseline { get; init; }

        public double Width { get; init; }

        public double WidthIncludingTrailingWhitespace { get; init; }

        public double Height { get; init; }

        public int TrailingWhitespaceLength { get; init; }

        public int NewLineLength { get; init; }

        public int FirstCluster { get; init; }

        public int LastCluster { get; init; }
    }
}
