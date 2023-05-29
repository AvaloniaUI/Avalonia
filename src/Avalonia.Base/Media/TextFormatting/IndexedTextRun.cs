namespace Avalonia.Media.TextFormatting
{
    internal class IndexedTextRun
    {
        public int TextSourceCharacterIndex { get; init; }
        public int RunIndex { get; set; }
        public int NextRunIndex { get; set; }
        public TextRun? TextRun { get; init; }
    }
}
