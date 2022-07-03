using System.Collections.Generic;

namespace Avalonia.Media.TextFormatting
{
    public class TextLineBreak
    {
        public TextLineBreak(TextEndOfLine? textEndOfLine = null, FlowDirection flowDirection = FlowDirection.LeftToRight, 
            IReadOnlyList<DrawableTextRun>? remainingRuns = null)
        {
            TextEndOfLine = textEndOfLine;
            FlowDirection = flowDirection;
            RemainingRuns = remainingRuns;
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
        /// Get the remaining runs that were split up by the <see cref="TextFormatter"/> during the formatting process.
        /// </summary>
        public IReadOnlyList<DrawableTextRun>? RemainingRuns { get; }
    }
}
