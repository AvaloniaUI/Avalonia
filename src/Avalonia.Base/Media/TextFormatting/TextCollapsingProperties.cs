using System.Collections.Generic;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Properties of text collapsing.
    /// </summary>
    public abstract class TextCollapsingProperties
    {
        /// <summary>
        /// Gets the width in which the collapsible range is constrained to.
        /// </summary>
        public abstract double Width { get; }

        /// <summary>
        /// Gets the text run that is used as collapsing symbol.
        /// </summary>
        public abstract TextRun Symbol { get; }

        /// <summary>
        /// Gets the flow direction that is used for collapsing.
        /// </summary>
        public abstract FlowDirection FlowDirection { get; }

        /// <summary>
        /// Collapses given text line.
        /// </summary>
        /// <param name="textLine">Text line to collapse.</param>
        public abstract TextRun[]? Collapse(TextLine textLine);

        /// <summary>
        /// Creates a list of runs for given collapsed length which includes specified symbol at the end.
        /// </summary>
        /// <param name="textLine">The text line.</param>
        /// <param name="collapsedLength">The collapsed length.</param>
        /// <param name="flowDirection">The flow direction.</param>
        /// <param name="shapedSymbol">The symbol.</param>
        /// <returns>List of remaining runs.</returns>
        public static TextRun[] CreateCollapsedRuns(TextLine textLine, int collapsedLength,
            FlowDirection flowDirection, TextRun shapedSymbol)
        {
            var textRuns = textLine.TextRuns;

            if (collapsedLength <= 0)
            {
                return new[] { shapedSymbol };
            }

            if (flowDirection == FlowDirection.RightToLeft)
            {
                collapsedLength = textLine.Length - collapsedLength;
            }

            var objectPool = FormattingObjectPool.Instance;

            var (preSplitRuns, postSplitRuns) = TextFormatterImpl.SplitTextRuns(textRuns, collapsedLength, objectPool);

            try
            {
                if (flowDirection == FlowDirection.RightToLeft)
                {
                    var collapsedRuns = new TextRun[postSplitRuns!.Count + 1];
                    postSplitRuns.CopyTo(collapsedRuns, 1);
                    collapsedRuns[0] = shapedSymbol;
                    return collapsedRuns;
                }
                else
                {
                    var collapsedRuns = new TextRun[preSplitRuns!.Count + 1];
                    preSplitRuns.CopyTo(collapsedRuns);
                    collapsedRuns[collapsedRuns.Length - 1] = shapedSymbol;
                    return collapsedRuns;
                }
            }
            finally
            {
                objectPool.TextRunLists.Return(ref preSplitRuns);
                objectPool.TextRunLists.Return(ref postSplitRuns);
            }
        }
    }
}
