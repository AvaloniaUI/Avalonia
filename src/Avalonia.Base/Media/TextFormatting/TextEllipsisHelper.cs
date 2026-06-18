using Avalonia.Media.TextFormatting.Unicode;

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

                                return TextCollapsingProperties.CreateCollapsedRuns(textLine, collapsedLength, shapedSymbol);
                            }

                            availableWidth -= textRunWidth;

                            break;
                        }

                    case DrawableTextRun drawableRun:
                        {
                            //The whole run needs to fit into available space
                            if (drawableRun.Size.Width > availableWidth)
                            {
                                return TextCollapsingProperties.CreateCollapsedRuns(textLine, collapsedLength, shapedSymbol);
                            }

                            availableWidth -= drawableRun.Size.Width;

                            break;
                        }
                }

                collapsedLength += currentRun.Length;
            }

            return null;
        }
    }
}
