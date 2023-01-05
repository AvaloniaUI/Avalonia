using System.Collections.Generic;
using Avalonia.Media.TextFormatting.Unicode;

namespace Avalonia.Media.TextFormatting
{
    internal static class TextEllipsisHelper
    {
        public static List<DrawableTextRun>? Collapse(TextLine textLine, TextCollapsingProperties properties, bool isWordEllipsis)
        {
            if (textLine.TextRuns is not List<DrawableTextRun> textRuns || textRuns.Count == 0)
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
                return new List<DrawableTextRun>(0);
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

                                        var text = new CharacterBufferRange(currentRun.CharacterBufferReference, currentRun.Length);

                                        var lineBreaker = new LineBreakEnumerator(text);

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

                                var collapsedRuns = new List<DrawableTextRun>(textRuns.Count);

                                if (collapsedLength > 0)
                                {
                                    var splitResult = TextFormatterImpl.SplitDrawableRuns(textRuns, collapsedLength);

                                    collapsedRuns.AddRange(splitResult.First);
                                }

                                collapsedRuns.Add(shapedSymbol);

                                return collapsedRuns;
                            }

                            availableWidth -= currentRun.Size.Width;


                            break;
                        }

                    case { } drawableRun:
                        {
                            //The whole run needs to fit into available space
                            if (currentWidth + drawableRun.Size.Width > availableWidth)
                            {
                                var collapsedRuns = new List<DrawableTextRun>(textRuns.Count);

                                if (collapsedLength > 0)
                                {
                                    var splitResult = TextFormatterImpl.SplitDrawableRuns(textRuns, collapsedLength);

                                    collapsedRuns.AddRange(splitResult.First);
                                }

                                collapsedRuns.Add(shapedSymbol);

                                return collapsedRuns;
                            }

                            break;
                        }
                }

                collapsedLength += currentRun.Length;

                runIndex++;
            }

            return null;
        }
    }
}
