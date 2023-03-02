using System;
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

            if (properties.Width < shapedSymbol.GlyphRun.Bounds.Width)
            {
                //Not enough space to fit in the symbol
                return Array.Empty<TextRun>();
            }

            var availableWidth = properties.Width - shapedSymbol.Size.Width;

            if(properties.FlowDirection== FlowDirection.LeftToRight)
            {
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

                                            while (currentBreakPosition < measuredLength && lineBreaker.MoveNext(out var lineBreak))
                                            {
                                                var nextBreakPosition = lineBreak.PositionMeasure;

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

                                    return CreateCollapsedRuns(textLine, collapsedLength, FlowDirection.LeftToRight, shapedSymbol);
                                }

                                availableWidth -= shapedRun.Size.Width;

                                break;
                            }

                        case DrawableTextRun drawableRun:
                            {
                                //The whole run needs to fit into available space
                                if (currentWidth + drawableRun.Size.Width > availableWidth)
                                {
                                    return CreateCollapsedRuns(textLine, collapsedLength, FlowDirection.LeftToRight, shapedSymbol);
                                }

                                availableWidth -= drawableRun.Size.Width;

                                break;
                            }
                    }

                    collapsedLength += currentRun.Length;

                    runIndex++;
                }
            }
            else
            {
                runIndex = textRuns.Count - 1;

                while (runIndex >= 0)
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

                                            while (currentBreakPosition < measuredLength && lineBreaker.MoveNext(out var lineBreak))
                                            {
                                                var nextBreakPosition = lineBreak.PositionMeasure;

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

                                    return CreateCollapsedRuns(textLine, collapsedLength, FlowDirection.RightToLeft, shapedSymbol);
                                }

                                availableWidth -= shapedRun.Size.Width;

                                break;
                            }

                        case DrawableTextRun drawableRun:
                            {
                                //The whole run needs to fit into available space
                                if (currentWidth + drawableRun.Size.Width > availableWidth)
                                {
                                    return CreateCollapsedRuns(textLine, collapsedLength, FlowDirection.RightToLeft, shapedSymbol);
                                }

                                availableWidth -= drawableRun.Size.Width;

                                break;
                            }
                    }

                    collapsedLength += currentRun.Length;

                    runIndex--;
                }
            }
      
            return null;
        }

        private static TextRun[] CreateCollapsedRuns(TextLine textLine, int collapsedLength,
            FlowDirection flowDirection, TextRun shapedSymbol)
        {
            var textRuns = textLine.TextRuns;

            if (collapsedLength <= 0)
            {
                return new[] { shapedSymbol };
            }

            if(flowDirection == FlowDirection.RightToLeft)
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
