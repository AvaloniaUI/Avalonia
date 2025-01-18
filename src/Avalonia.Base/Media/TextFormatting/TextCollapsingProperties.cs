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
        /// Collapses given text runs.
        /// </summary>
        /// <param name="textRuns">The text runs to collapse.</param>
        public abstract TextRun[]? Collapse(TextRun[] textRuns);

        /// <summary>
        /// Creates a list of runs for given collapsed length which includes specified symbol at the end.
        /// </summary>
        /// <param name="textRuns">The text runs</param>
        /// <param name="collapsedLength">The collapsed length.</param>
        /// <param name="shapedSymbol">The symbol.</param>
        /// <returns>List of remaining runs.</returns>
        public static TextRun[] CreateCollapsedRuns(TextRun[] textRuns, int collapsedLength, TextRun shapedSymbol)
        {
            if (collapsedLength <= 0 || textRuns.Length == 0)
            {
                return [shapedSymbol];
            }

            var objectPool = FormattingObjectPool.Instance;

            var (preSplitRuns, postSplitRuns) = TextFormatterImpl.SplitTextRuns(textRuns, collapsedLength, objectPool);

            try
            {
                var collapsedRuns = new TextRun[preSplitRuns!.Count + 1];

                preSplitRuns.CopyTo(collapsedRuns);
                collapsedRuns[collapsedRuns.Length - 1] = shapedSymbol;

                return collapsedRuns;
            }
            finally
            {
                objectPool.TextRunLists.Return(ref preSplitRuns);
                objectPool.TextRunLists.Return(ref postSplitRuns);
            }
        }
    }
}
