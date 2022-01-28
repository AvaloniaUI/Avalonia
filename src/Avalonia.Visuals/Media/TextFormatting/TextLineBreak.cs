using System.Collections.Generic;

namespace Avalonia.Media.TextFormatting
{
    public class TextLineBreak
    {
        public TextLineBreak(TextEndOfLine? textEndOfLine = null, FlowDirection flowDirection = FlowDirection.LeftToRight, 
            IReadOnlyList<ShapedTextCharacters>? remainingCharacters = null)
        {
            TextEndOfLine = textEndOfLine;
            FlowDirection = flowDirection;
            RemainingCharacters = remainingCharacters;
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
        /// Get the remaining shaped characters that were split up by the <see cref="TextFormatter"/> during the formatting process.
        /// </summary>
        public IReadOnlyList<ShapedTextCharacters>? RemainingCharacters { get; }
    }
}
