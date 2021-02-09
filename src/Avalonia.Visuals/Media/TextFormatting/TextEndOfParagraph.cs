namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    ///  A text run that indicates the end of a paragraph.
    /// </summary>
    public class TextEndOfParagraph : TextEndOfLine
    {
        public TextEndOfParagraph() : this(DefaultTextSourceLength) { }

        public TextEndOfParagraph(int textSourceLength) : base(textSourceLength)
        {
        }
    }
}
