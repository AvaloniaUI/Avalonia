using System.Collections.Generic;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting
{
    internal static class TextEllipsisHelper
    {
        public static TextRun[]? Collapse(TextLine textLine, TextCollapsingProperties properties, bool isWordEllipsis)
        {
            var textRunsEnumerator = new LogicalTextRunEnumerator(textLine);

            if (textRunsEnumerator.Count == 0)
            {
                return null;
            }

            var shapedSymbol = TextFormatter.CreateSymbol(properties.Symbol, properties.FlowDirection);

            if (properties.Width < shapedSymbol.GlyphRun.Bounds.Width)
            {
                //Not enough space to fit in the symbol
                shapedSymbol.Dispose();

                return [];
            }

            var availableWidth = properties.Width - shapedSymbol.Size.Width;

            var collapsedLength = 0;

            while (textRunsEnumerator.MoveNext(out var currentRun))
            {
                switch (currentRun)
                {
                    case ShapedTextRun shapedRun:
                        {
                            var textRunWidth = shapedRun.Size.Width;

                            if (textRunWidth > availableWidth)
                            {
                                if (shapedRun.TryMeasureCharacters(availableWidth, out var measuredLength))
                                {
                                    if (isWordEllipsis && measuredLength < textLine.Length)
                                    {
                                        var currentBreakPosition = 0;

                                        var text = currentRun.Text.Span;
                                        var wordBreaker = new WordBreakEnumerator(text);

                                        while (currentBreakPosition < measuredLength && wordBreaker.MoveNext(out var wordSegment))
                                        {
                                            // TODO13: a WordSegment can carry an empty Text, so Length is the only
                                            // code-unit width valid for every segment.
                                            var nextBreakPosition = wordSegment.Offset + wordSegment.Length;

                                            if (nextBreakPosition == 0)
                                            {
                                                break;
                                            }

                                            if (nextBreakPosition >= measuredLength)
                                            {
                                                break;
                                            }

                                            var firstCodepoint = Codepoint.ReadAt(text, wordSegment.Offset, out _);

                                            if (firstCodepoint.WordBreakClass == WordBreakClass.WSegSpace)
                                            {
                                                // UAX #29 allows boundaries on both sides of WSegSpace; use the start
                                                // to preserve the existing behavior of trimming spaces before the ellipsis.
                                                currentBreakPosition = wordSegment.Offset;

                                                continue;
                                            }

                                            currentBreakPosition = nextBreakPosition;
                                        }

                                        measuredLength = currentBreakPosition;
                                    }
                                }

                                collapsedLength += measuredLength;

                                return TextCollapsingProperties.CreateOwnedCollapsedRuns(textLine, collapsedLength, shapedSymbol);
                            }

                            availableWidth -= textRunWidth;

                            break;
                        }

                    case DrawableTextRun drawableRun:
                        {
                            //The whole run needs to fit into available space
                            if (drawableRun.Size.Width > availableWidth)
                            {
                                return TextCollapsingProperties.CreateOwnedCollapsedRuns(textLine, collapsedLength, shapedSymbol);
                            }

                            availableWidth -= drawableRun.Size.Width;

                            break;
                        }
                }

                collapsedLength += currentRun.Length;
            }

            shapedSymbol.Dispose();

            return null;
        }

        /// <summary>
        /// Appends the longest logical suffix of <paramref name="logicalRuns"/> that fits into
        /// <paramref name="availableWidth"/> to <paramref name="target"/>, in logical order.
        /// </summary>
        /// <remarks>
        /// Takes over the reference held to each shaped run in <paramref name="logicalRuns"/>: a run
        /// that is kept moves into <paramref name="target"/>, every other run is released.
        /// Runs are taken whole from the logical end while they fit. The first run that does not fit
        /// contributes its fitting trailing characters if it is shaped text, nothing if it is an inline
        /// object, and ends the walk. Stopping there keeps the result contiguous: with mixed fonts an
        /// earlier run can still have characters narrow enough for the leftover width, and taking them
        /// would join text that is not adjacent in the source.
        /// </remarks>
        public static void AppendFittingSuffix(IReadOnlyList<TextRun> logicalRuns, double availableWidth,
            List<TextRun> target)
        {
            var start = target.Count;

            var i = logicalRuns.Count - 1;

            while (i >= 0 && TryAppendFromEnd(logicalRuns[i], ref availableWidth, target))
            {
                i--;
            }

            // The run that stopped the walk has already handed on or released its reference; the
            // runs before it are not shown.
            for (i--; i >= 0; i--)
            {
                TextCollapsingProperties.Release(logicalRuns[i]);
            }

            // The walk appends from the logical end backwards.
            target.Reverse(start, target.Count - start);
        }

        /// <summary>
        /// Appends <paramref name="run"/>, or the part of it that fits, and returns whether the whole
        /// run fit so the walk can continue with the run before it.
        /// </summary>
        private static bool TryAppendFromEnd(TextRun run, ref double availableWidth, List<TextRun> target)
        {
            switch (run)
            {
                case ShapedTextRun shapedRun:
                    {
                        var width = shapedRun.Size.Width;

                        if (!MathUtilities.GreaterThan(width, availableWidth))
                        {
                            target.Add(shapedRun);
                            availableWidth -= width;

                            return true;
                        }

                        if (shapedRun.TryMeasureCharactersBackwards(availableWidth, out var length, out _) &&
                            length > 0)
                        {
                            if (length >= shapedRun.Length)
                            {
                                target.Add(shapedRun);

                                return false;
                            }

                            // Split hands out its own references to both halves.
                            var (head, tail) = shapedRun.Split(shapedRun.Length - length);

                            head?.Dispose();
                            target.Add(tail!);
                        }

                        shapedRun.Dispose();

                        return false;
                    }
                case DrawableTextRun drawableRun:
                    {
                        var width = drawableRun.Size.Width;

                        if (MathUtilities.GreaterThan(width, availableWidth))
                        {
                            return false;
                        }

                        target.Add(drawableRun);
                        availableWidth -= width;

                        return true;
                    }
                default:
                    {
                        // End-of-line runs take no width.
                        target.Add(run);

                        return true;
                    }
            }
        }
    }
}
