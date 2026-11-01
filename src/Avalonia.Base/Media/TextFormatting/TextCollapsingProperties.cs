using System;

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
        /// Collapses the given text line and returns the resulting runs, or
        /// <see langword="null"/> if no collapse is needed (the consumer
        /// then keeps the original line unchanged).
        /// </summary>
        /// <param name="textLine">Text line to collapse.</param>
        /// <remarks>
        /// Implementations MUST return runs in <b>logical order</b>. The
        /// consumer (<c>TextLineImpl.Collapse</c>) wraps the returned array
        /// in a new <see cref="TextLine"/> and re-runs the BiDi reorderer
        /// via <c>FinalizeLine</c>, so pre-applying visual order here would
        /// be reordered a second time and produce garbled output on RTL or
        /// mixed-bidi lines.
        /// <para>
        /// Iterate the source line's runs via
        /// <c>LogicalTextRunEnumerator</c>, not <see cref="TextLine.TextRuns"/>
        /// (which is post-bidi visual order). Use
        /// <see cref="CreateCollapsedRuns(TextLine, int, DrawableTextRun?)"/> when an implementation only
        /// needs the standard "logical prefix + symbol" shape.
        /// </para>
        /// </remarks>
        public abstract TextRun[]? Collapse(TextLine textLine);

        /// <inheritdoc cref="CreateCollapsedRuns(TextLine, int, DrawableTextRun?)"/>
        [Obsolete("Use the new overload demanding DrawableTextRun.")]
        public static TextRun[] CreateCollapsedRuns(TextLine textLine, int collapsedLength, TextRun shapedSymbol) => CreateCollapsedRunsImpl(textLine, collapsedLength, shapedSymbol);

        /// <summary>
        /// Creates a list of runs for given collapsed length which includes specified symbol at the end.
        /// </summary>
        /// <param name="textLine">The text line.</param>
        /// <param name="collapsedLength">The collapsed length.</param>
        /// <param name="collapseSymbol">The symbol used to indicate that characters have been removed.</param>
        /// <returns>List of remaining runs.</returns>
        public static TextRun[] CreateCollapsedRuns(TextLine textLine, int collapsedLength, DrawableTextRun? collapseSymbol) => CreateCollapsedRunsImpl(textLine, collapsedLength, collapseSymbol);

        private static TextRun[] CreateCollapsedRunsImpl(TextLine textLine, int collapsedLength, TextRun? collapseSymbol)
        {
            if (collapsedLength <= 0)
            {
                return collapseSymbol switch
                {
                    DrawableTextRun drawable => [new CollapseSymbolRun(textLine.TextRuns, drawable)],
                    null => [],
                    _ => [collapseSymbol], // legacy behaviour
                };
            }

            var objectPool = FormattingObjectPool.Instance;

            FormattingObjectPool.RentedList<TextRun>? preSplitRuns = null;
            FormattingObjectPool.RentedList<TextRun>? postSplitRuns = null;

            var textRuns = objectPool.TextRunLists.Rent();

            try
            {
                var textRunEnumerator = new LogicalTextRunEnumerator(textLine);

                textRuns.EnsureCapacity(textRunEnumerator.Count);
                while (textRunEnumerator.MoveNext(out var textRun))
                {
                    textRuns.Add(textRun);
                }

                (preSplitRuns, postSplitRuns) = TextFormatterImpl.SplitTextRuns(textRuns, collapsedLength, objectPool);

                return collapseSymbol switch
                {
                    DrawableTextRun drawable => [.. preSplitRuns!, new CollapseSymbolRun([.. postSplitRuns ?? []], drawable)],
                    null => [.. preSplitRuns!],
                    _ => [.. preSplitRuns!, collapseSymbol],  // legacy behaviour
                };
            }
            finally
            {
                objectPool.TextRunLists.Return(ref textRuns);
                objectPool.TextRunLists.Return(ref preSplitRuns);
                objectPool.TextRunLists.Return(ref postSplitRuns);
            }
        }
    }
}
