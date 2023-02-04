namespace Avalonia.Media.TextFormatting
{
    public class TextLineBreak
    {
        public TextLineBreak(TextEndOfLine? textEndOfLine = null,
            FlowDirection flowDirection = FlowDirection.LeftToRight, bool isSplit = false)
        {
            TextEndOfLine = textEndOfLine;
            FlowDirection = flowDirection;
            IsSplit = isSplit;
        }

        /// <summary>
        /// Get the end of line run.
        /// </summary>
        public TextEndOfLine? TextEndOfLine { get; }

        /// <summary>
        /// Get the flow direction for remaining characters.
        /// </summary>
        public FlowDirection FlowDirection { get; }
        
        /// <summary>
        /// Gets whether there were remaining runs after this line break,
        /// that were split up by the <see cref="TextFormatter"/> during the formatting process.
        /// </summary>
        public bool IsSplit { get; }
    }
}
