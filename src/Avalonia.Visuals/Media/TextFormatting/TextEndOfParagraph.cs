namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    ///  A text run that indicates the end of a paragraph.
    /// </summary>
    public class TextEndOfParagraph : TextEndOfLine
    {
        public TextEndOfParagraph() { }

        public TextEndOfParagraph(int textSourceLength)
        {
            TextSourceLength = textSourceLength;
        }

        public override int TextSourceLength { get; }
    }
}
