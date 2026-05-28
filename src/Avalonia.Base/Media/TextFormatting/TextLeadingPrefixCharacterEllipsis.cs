// ReSharper disable ForCanBeConvertedToForeach
using System;
using System.Collections.Generic;
using static Avalonia.Media.TextFormatting.FormattingObjectPool;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Ellipsis based on a fixed length leading prefix and suffix growing from the end at character granularity.
    /// </summary>
    public sealed class TextLeadingPrefixCharacterEllipsis : TextCollapsingProperties
    {
        private readonly int _prefixLength;

        /// <summary>
        /// Construct a text trailing word ellipsis collapsing properties.
        /// </summary>
        /// <param name="ellipsis">Text used as collapsing symbol.</param>
        /// <param name="prefixLength">Length of leading prefix.</param>
        /// <param name="width">width in which collapsing is constrained to</param>
        /// <param name="textRunProperties">text run properties of ellipsis symbol</param>
        /// <param name="flowDirection">the flow direction of the collapes line.</param>
        public TextLeadingPrefixCharacterEllipsis(
            string ellipsis,
            int prefixLength,
            double width,
            TextRunProperties textRunProperties,
            FlowDirection flowDirection)
        {
            if (prefixLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(prefixLength));
            }

            _prefixLength = prefixLength;
            Width = width;
            Symbol = new TextCharacters(ellipsis, textRunProperties);
            FlowDirection = flowDirection;
        }

        /// <inheritdoc/>
        public override double Width { get; }

        /// <inheritdoc/>
        public override TextRun Symbol { get; }

        public override FlowDirection FlowDirection { get; }

        /// <inheritdoc />
        public override TextRun[]? Collapse(TextLine textLine)
        {
            // Materialize runs in LOGICAL order. The consumer (TextLineImpl.Collapse)
            // wraps our result in a new TextLine and runs the BiDi reorderer via
            // FinalizeLine, so we must hand back runs in logical order — not the
            // visual order exposed via textLine.TextRuns.
            var objectPool = FormattingObjectPool.Instance;
            var logicalRuns = objectPool.TextRunLists.Rent();

            try
            {
                var enumerator = new LogicalTextRunEnumerator(textLine);
                while (enumerator.MoveNext(out var r))
                {
                    logicalRuns.Add(r);
                }

                var shapedSymbol = TextFormatter.CreateSymbol(Symbol, FlowDirection);

                if (Width < shapedSymbol.GlyphRun.Bounds.Width)
                {
                    return Array.Empty<TextRun>();
                }

                // Overview of ellipsis structure
                // Prefix length run | Ellipsis symbol | Post split run growing from the end |
                var totalBudget = Width - shapedSymbol.Size.Width;
                var availableWidth = totalBudget;
                var charsBeforeCurrentRun = 0;

                for (var runIndex = 0; runIndex < logicalRuns.Count; runIndex++)
                {
                    var currentRun = logicalRuns[runIndex];

                    switch (currentRun)
                    {
                        case ShapedTextRun shapedRun:
                            {
                                // Per-run check: does THIS run alone exceed what's left?
                                // (The earlier `currentWidth +=` / `currentWidth > availableWidth`
                                //  pattern was comparing cumulative-so-far against budget-remaining,
                                //  which double-counted and tripped overflow far too early on
                                //  multi-run lines.)
                                if (shapedRun.Size.Width > availableWidth)
                                {
                                    shapedRun.TryMeasureCharacters(availableWidth, out var measuredLength);

                                    var totalFitChars = charsBeforeCurrentRun + measuredLength;

                                    if (totalFitChars > 0)
                                    {
                                        var collapsedRuns = objectPool.TextRunLists.Rent();

                                        RentedList<TextRun>? rentedPreSplitRuns = null;
                                        RentedList<TextRun>? rentedPostSplitRuns = null;
                                        RentedList<TextRun>? reversedSuffix = null;

                                        try
                                        {
                                            IReadOnlyList<TextRun>? effectivePostSplitRuns;

                                            // Split at GLOBAL character index totalFitChars-capped-by-prefixLength.
                                            // (Previously this used `Math.Min(_prefixLength, measuredLength)`
                                            //  treating per-run `measuredLength` as a global offset, which
                                            //  produced a prefix from the wrong characters on multi-run lines.)
                                            var prefixCutoff = Math.Min(_prefixLength, totalFitChars);

                                            if (prefixCutoff > 0)
                                            {
                                                (rentedPreSplitRuns, rentedPostSplitRuns) = TextFormatterImpl.SplitTextRuns(
                                                    logicalRuns, prefixCutoff, objectPool);

                                                effectivePostSplitRuns = rentedPostSplitRuns;

                                                if (rentedPreSplitRuns is not null)
                                                {
                                                    foreach (var preSplitRun in rentedPreSplitRuns)
                                                    {
                                                        collapsedRuns.Add(preSplitRun);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                effectivePostSplitRuns = logicalRuns;
                                            }

                                            collapsedRuns.Add(shapedSymbol);

                                            if (totalFitChars <= _prefixLength || effectivePostSplitRuns is null)
                                            {
                                                return collapsedRuns.ToArray();
                                            }

                                            // Suffix budget = total budget minus the actual prefix width.
                                            // (Previously this used the loop's `availableWidth` which had
                                            //  over-subtracted: it assumed entire fully-fitting runs went
                                            //  to the prefix, even when prefixLength capped the prefix
                                            //  partway through one of them. Deriving from the actual
                                            //  preSplit run widths gives the correct remaining budget.)
                                            var availableSuffixWidth = totalBudget;

                                            if (rentedPreSplitRuns is not null)
                                            {
                                                foreach (var run in rentedPreSplitRuns)
                                                {
                                                    switch (run)
                                                    {
                                                        case ShapedTextRun preShaped:
                                                            availableSuffixWidth -= preShaped.Size.Width;
                                                            break;
                                                        case DrawableTextRun preDrawable:
                                                            availableSuffixWidth -= preDrawable.Size.Width;
                                                            break;
                                                    }
                                                }
                                            }

                                            // Walk the post-split runs from the logical tail back toward the
                                            // prefix, fitting trailing characters into availableSuffixWidth.
                                            // We collect each split into reversedSuffix here (so the LAST
                                            // logical run lands at index 0) and then drain reversedSuffix
                                            // backwards when appending to collapsedRuns, which restores
                                            // LOGICAL order. FinalizeLine handles the visual re-bidi.
                                            reversedSuffix = objectPool.TextRunLists.Rent();

                                            for (var i = effectivePostSplitRuns.Count - 1; i >= 0; i--)
                                            {
                                                var run = effectivePostSplitRuns[i];

                                                switch (run)
                                                {
                                                    case ShapedTextRun endShapedRun:
                                                    {
                                                        if (endShapedRun.TryMeasureCharactersBackwards(availableSuffixWidth,
                                                                out var suffixCount, out var suffixWidth))
                                                        {
                                                            availableSuffixWidth -= suffixWidth;

                                                            if (suffixCount > 0)
                                                            {
                                                                var splitSuffix =
                                                                    endShapedRun.Split(run.Length - suffixCount);

                                                                reversedSuffix.Add(splitSuffix.Second!);
                                                            }
                                                        }

                                                        break;
                                                    }
                                                }
                                            }

                                            for (var i = reversedSuffix.Count - 1; i >= 0; i--)
                                            {
                                                collapsedRuns.Add(reversedSuffix[i]);
                                            }

                                            return collapsedRuns.ToArray();
                                        }
                                        finally
                                        {
                                            objectPool.TextRunLists.Return(ref rentedPreSplitRuns);
                                            objectPool.TextRunLists.Return(ref rentedPostSplitRuns);
                                            objectPool.TextRunLists.Return(ref reversedSuffix);
                                            objectPool.TextRunLists.Return(ref collapsedRuns);
                                        }
                                    }

                                    return new TextRun[] { shapedSymbol };
                                }

                                availableWidth -= shapedRun.Size.Width;

                                break;
                            }
                        case DrawableTextRun drawableTextRun:
                            {
                                availableWidth -= drawableTextRun.Size.Width;

                                break;
                            }
                    }

                    charsBeforeCurrentRun += currentRun.Length;
                }

                return null;
            }
            finally
            {
                objectPool.TextRunLists.Return(ref logicalRuns);
            }
        }
    }
}
