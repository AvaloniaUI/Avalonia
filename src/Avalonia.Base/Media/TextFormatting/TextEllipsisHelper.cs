using System;
using System.Collections.Generic;
using Avalonia.Media.TextFormatting.Unicode;

namespace Avalonia.Media.TextFormatting
{
    internal static class TextEllipsisHelper
    {
        public static TextRun[]? Collapse(TextLine textLine, TextCollapsingProperties properties, bool isWordEllipsis)
        {
            var textRuns = textLine.TextRuns;

            if (textRuns.Count == 0)
            {
                return null;
            }

            var runIndex = 0;
            var currentWidth = 0.0;
            var collapsedLength = 0;
            var shapedSymbol = TextFormatterImpl.CreateSymbol(properties.Symbol, FlowDirection.LeftToRight);

            if (properties.Width < shapedSymbol.GlyphRun.Size.Width)
            {
                //Not enough space to fit in the symbol
                return Array.Empty<TextRun>();
            }

            var availableWidth = properties.Width - shapedSymbol.Size.Width;

            while (runIndex < textRuns.Count)
            {
                var currentRun = textRuns[runIndex];

                switch (currentRun)
                {
                    case ShapedTextRun shapedRun:
                        {
                            currentWidth += shapedRun.Size.Width;

                            if (currentWidth > availableWidth)
                            {
                                if (shapedRun.TryMeasureCharacters(availableWidth, out var measuredLength))
                                {
                                    if (isWordEllipsis && measuredLength < textLine.Length)
                                    {
                                        var currentBreakPosition = 0;

                                        var lineBreaker = new LineBreakEnumerator(currentRun.Text.Span);

                                        while (currentBreakPosition < measuredLength && lineBreaker.MoveNext())
                                        {
                                            var nextBreakPosition = lineBreaker.Current.PositionMeasure;

                                            if (nextBreakPosition == 0)
                                            {
                                                break;
                                            }

                                            if (nextBreakPosition >= measuredLength)
                                            {
                                                break;
                                            }

                                            currentBreakPosition = nextBreakPosition;
                                        }

                                        measuredLength = currentBreakPosition;
                                    }
                                }

                                collapsedLength += measuredLength;

                                return CreateCollapsedRuns(textRuns, collapsedLength, shapedSymbol);
                            }

                            availableWidth -= shapedRun.Size.Width;

                            break;
                        }

                    case DrawableTextRun drawableRun:
                        {
                            //The whole run needs to fit into available space
                            if (currentWidth + drawableRun.Size.Width > availableWidth)
                            {
                                return CreateCollapsedRuns(textRuns, collapsedLength, shapedSymbol);
                            }

                            availableWidth -= drawableRun.Size.Width;

                            break;
                        }
                }

                collapsedLength += currentRun.Length;

                runIndex++;
            }

            return null;
        }

        private static TextRun[] CreateCollapsedRuns(IReadOnlyList<TextRun> textRuns, int collapsedLength,
            TextRun shapedSymbol)
        {
            if (collapsedLength <= 0)
            {
                return new[] { shapedSymbol };
            }

            var objectPool = FormattingObjectPool.Instance;

            var (preSplitRuns, postSplitRuns) = TextFormatterImpl.SplitTextRuns(textRuns, collapsedLength, objectPool);

            var collapsedRuns = new TextRun[preSplitRuns.Count + 1];
            preSplitRuns.CopyTo(collapsedRuns);
            collapsedRuns[collapsedRuns.Length - 1] = shapedSymbol;

            objectPool.TextRunLists.Return(ref preSplitRuns);
            objectPool.TextRunLists.Return(ref postSplitRuns);

            return collapsedRuns;
        }
    }
}
