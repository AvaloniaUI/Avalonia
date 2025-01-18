﻿using Avalonia.Media.TextFormatting.Unicode;

namespace Avalonia.Media.TextFormatting
{
    internal static class TextEllipsisHelper
    {
        public static TextRun[]? Collapse(TextRun[] textRuns, TextCollapsingProperties properties, bool isWordEllipsis)
        {
            if (textRuns.Length == 0)
            {
                return null;
            }

            var shapedSymbol = TextFormatter.CreateSymbol(properties.Symbol, properties.FlowDirection);

            if (properties.Width < shapedSymbol.GlyphRun.Bounds.Width)
            {
                //Not enough space to fit in the symbol
                return [];
            }

            var textLength = 0;

            for (var i = 0; i < textRuns.Length; i++)
            {
                textLength += textRuns[i].Length;
            }

            var availableWidth = properties.Width - shapedSymbol.Size.Width;

            var collapsedLength = 0;

            for (var i = 0; i < textRuns.Length; i++)
            {
                var currentRun = textRuns[i];

                switch (currentRun)
                {
                    case ShapedTextRun shapedRun:
                        {
                            var textRunWidth = shapedRun.Size.Width;

                            if (textRunWidth > availableWidth)
                            {
                                if (shapedRun.TryMeasureCharacters(availableWidth, out var measuredLength))
                                {
                                    if (isWordEllipsis && measuredLength < textLength)
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

                                return TextCollapsingProperties.CreateCollapsedRuns(textRuns, collapsedLength, shapedSymbol);
                            }

                            availableWidth -= textRunWidth;

                            break;
                        }

                    case DrawableTextRun drawableRun:
                        {
                            //The whole run needs to fit into available space
                            if (drawableRun.Size.Width > availableWidth)
                            {
                                return TextCollapsingProperties.CreateCollapsedRuns(textRuns, collapsedLength, shapedSymbol);
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
