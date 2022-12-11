namespace Avalonia.Media
{
    public readonly record struct GlyphRunMetrics
    {
        public GlyphRunMetrics(double width, double widthIncludingTrailingWhitespace, double height,
            int trailingWhitespaceLength, int newLineLength, int firstCluster, int lastCluster)
        {
            Width = width;
            WidthIncludingTrailingWhitespace = widthIncludingTrailingWhitespace;
            Height = height;
            TrailingWhitespaceLength = trailingWhitespaceLength;
            NewLineLength= newLineLength;
            FirstCluster = firstCluster;
            LastCluster = lastCluster;
        }

        public double Width { get; }

        public double WidthIncludingTrailingWhitespace { get; }

        public double Height { get; }

        public int TrailingWhitespaceLength { get; }
        
        public int NewLineLength { get;  }

        public int FirstCluster { get; }

        public int LastCluster { get; }
    }
}
