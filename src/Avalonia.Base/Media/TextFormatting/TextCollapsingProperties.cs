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
        /// <see cref="CreateCollapsedRuns"/> when an implementation only
        /// needs the standard "logical prefix + symbol" shape.
        /// </para>
        /// <para>
        /// The returned runs may be in any arrangement, for example a leading or middle
        /// symbol, and may reuse runs of <paramref name="textLine"/> as they are. The collapsed
        /// line takes its own reference to each reused run, so disposing either line leaves the
        /// other intact. Every other run in the result, such as the symbol from
        /// <see cref="TextFormatter.CreateSymbol"/>, passes to the collapsed line, which
        /// disposes it with itself; dispose the symbol when the result does not contain it.
        /// </para>
        /// </remarks>
        public abstract TextRun[]? Collapse(TextLine textLine);

        /// <summary>
        /// Gets whether <see cref="Collapse"/> already holds a reference for the collapsed line
        /// to every run it returns, including the runs it reuses from the source line. The
        /// collapsed line then takes the result as it is instead of looking up which runs it
        /// shares with the source line.
        /// </summary>
        internal virtual bool ReturnsOwnedRuns => false;

        /// <summary>
        /// Creates a list of runs for given collapsed length which includes specified symbol at the end.
        /// </summary>
        /// <param name="textLine">The text line.</param>
        /// <param name="collapsedLength">The collapsed length.</param>
        /// <param name="shapedSymbol">The symbol. The returned runs take it over.</param>
        /// <returns>The logical prefix of <paramref name="textLine"/> followed by the symbol.</returns>
        public static TextRun[] CreateCollapsedRuns(TextLine textLine, int collapsedLength, TextRun shapedSymbol)
            => CreateCollapsedRunsCore(textLine, collapsedLength, shapedSymbol, owned: false);

        /// <summary>
        /// Creates the same runs as <see cref="CreateCollapsedRuns"/>,
        /// holding a reference to each run reused from <paramref name="textLine"/>, for
        /// collapsers that set <see cref="ReturnsOwnedRuns"/>.
        /// </summary>
        internal static TextRun[] CreateOwnedCollapsedRuns(TextLine textLine, int collapsedLength, TextRun shapedSymbol)
            => CreateCollapsedRunsCore(textLine, collapsedLength, shapedSymbol, owned: true);

        private static TextRun[] CreateCollapsedRunsCore(TextLine textLine, int collapsedLength, TextRun shapedSymbol,
            bool owned)
        {
            if (collapsedLength <= 0)
            {
                return [shapedSymbol];
            }

            var objectPool = FormattingObjectPool.Instance;

            FormattingObjectPool.RentedList<TextRun>? preSplitRuns = null;
            FormattingObjectPool.RentedList<TextRun>? postSplitRuns = null;

            var textRuns = objectPool.TextRunLists.Rent();

            try
            {
                var textRunEnumerator = new LogicalTextRunEnumerator(textLine);

                var textRunsLength = 0;

                while (textRunEnumerator.MoveNext(out var textRun))
                {
                    if (textRunsLength >= collapsedLength)
                    {
                        break;
                    }

                    textRunsLength += textRun.Length;

                    textRuns.Add(AddRef(textRun));
                }

                // The split consumes the references taken above: the leading part moves into
                // the result, the rest is not shown.
                (preSplitRuns, postSplitRuns) = TextFormatterImpl.SplitTextRuns(textRuns, collapsedLength, objectPool);

                Release(postSplitRuns);

                if (!owned)
                {
                    // The prefix matches the source runs position by position up to the split
                    // run, so the runs reused unsplit are found without a search. The collapsed
                    // line takes its own reference to them.
                    for (var i = 0; i < preSplitRuns!.Count && i < textRuns.Count; i++)
                    {
                        if (ReferenceEquals(preSplitRuns[i], textRuns[i]))
                        {
                            Release(textRuns[i]);
                        }
                    }
                }

                var collapsedRuns = new TextRun[preSplitRuns!.Count + 1];

                preSplitRuns.CopyTo(collapsedRuns);
                collapsedRuns[collapsedRuns.Length - 1] = shapedSymbol;

                return collapsedRuns;
            }
            finally
            {
                objectPool.TextRunLists.Return(ref textRuns);
                objectPool.TextRunLists.Return(ref preSplitRuns);
                objectPool.TextRunLists.Return(ref postSplitRuns);
            }
        }

        /// <summary>
        /// Rents a list of the runs of <paramref name="textLine"/> in logical order, holding
        /// one reference to each shaped run. The caller passes each reference on to the
        /// collapsed result or releases it.
        /// </summary>
        internal static FormattingObjectPool.RentedList<TextRun> RentLogicalRuns(TextLine textLine,
            FormattingObjectPool objectPool)
        {
            var runs = objectPool.TextRunLists.Rent();
            var enumerator = new LogicalTextRunEnumerator(textLine);

            while (enumerator.MoveNext(out var run))
            {
                runs.Add(AddRef(run));
            }

            return runs;
        }

        /// <summary>
        /// Gets whether <paramref name="run"/> is one of the runs of <paramref name="textLine"/>.
        /// </summary>
        internal static bool IsSourceRun(TextLine textLine, TextRun run)
        {
            var sourceRuns = textLine.TextRuns;

            for (var i = 0; i < sourceRuns.Count; i++)
            {
                if (ReferenceEquals(sourceRuns[i], run))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Takes an additional reference to <paramref name="run"/> when it is a shaped run.
        /// </summary>
        internal static TextRun AddRef(TextRun run) => run is ShapedTextRun shapedRun ? shapedRun.AddRef() : run;

        /// <summary>
        /// Releases the reference held to <paramref name="run"/> when it is a shaped run.
        /// </summary>
        internal static void Release(TextRun? run) => (run as ShapedTextRun)?.Dispose();

        /// <summary>
        /// Releases the references held to the shaped runs in <paramref name="runs"/>.
        /// </summary>
        internal static void Release(IReadOnlyList<TextRun>? runs)
        {
            if (runs is null)
            {
                return;
            }

            for (var i = 0; i < runs.Count; i++)
            {
                Release(runs[i]);
            }
        }
    }
}
