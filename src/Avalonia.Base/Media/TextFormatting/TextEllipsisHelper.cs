using System;
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
            var trailingWhitespaceLength = 0;

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
                                    else if (!isWordEllipsis)
                                    {
                                        measuredLength -= GetTrailingWhitespaceLength(shapedRun.Text.Span.Slice(0, measuredLength));
                                    }
                                }

                                collapsedLength += measuredLength;

                                if (measuredLength == 0)
                                {
                                    collapsedLength -= trailingWhitespaceLength;
                                }

                                return TextCollapsingProperties.CreateCollapsedRuns(textLine, collapsedLength, shapedSymbol);
                            }

                            availableWidth -= textRunWidth;

                            if (!isWordEllipsis)
                            {
                                var runText = shapedRun.Text.Span;
                                var runTrailingWhitespaceLength = GetTrailingWhitespaceLength(runText);

                                trailingWhitespaceLength = runTrailingWhitespaceLength == runText.Length ?
                                    trailingWhitespaceLength + runTrailingWhitespaceLength :
                                    runTrailingWhitespaceLength;
                            }

                            break;
                        }

                    case DrawableTextRun drawableRun:
                        {
                            //The whole run needs to fit into available space
                            if (drawableRun.Size.Width > availableWidth)
                            {
                                return TextCollapsingProperties.CreateCollapsedRuns(textLine, collapsedLength - trailingWhitespaceLength, shapedSymbol);
                            }

                            availableWidth -= drawableRun.Size.Width;
                            trailingWhitespaceLength = 0;

                            break;
                        }
                }

                collapsedLength += currentRun.Length;
            }

            return null;
        }

        private static int GetTrailingWhitespaceLength(ReadOnlySpan<char> text)
        {
            var length = 0;

            while (length < text.Length && char.IsWhiteSpace(text[text.Length - 1 - length]))
            {
                length++;
            }

            return length;
        }
    }
}
