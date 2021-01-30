namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Specialized text run used to mark the end of a segment, i.e., to end
    /// the scope affected by a preceding TextModifier run.
    /// </summary>
    public class TextEndOfSegment : TextRun
    {
        public TextEndOfSegment(int textSourceLength)
        {
            TextSourceLength = textSourceLength;
        }

        public override int TextSourceLength { get; }
    }
}
