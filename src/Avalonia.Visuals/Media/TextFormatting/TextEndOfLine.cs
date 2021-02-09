namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// A text run that indicates the end of a line.
    /// </summary>
    public class TextEndOfLine : TextRun
    {
        public TextEndOfLine() : this(DefaultTextSourceLength)
        {
        }

        public TextEndOfLine(int textSourceLength)
        {
            TextSourceLength = textSourceLength;
        }

        public override int TextSourceLength { get; }
    }
}
