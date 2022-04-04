using System.Collections.Generic;
using Avalonia.Media.TextFormatting.Unicode;

namespace Avalonia.Media.TextFormatting
{
    internal class TextEllipsisHelper
    {
        public static List<ShapedTextCharacters>? Collapse(TextLine textLine, TextCollapsingProperties properties, bool isWordEllipsis)
        {
            var shapedTextRuns = textLine.TextRuns as List<ShapedTextCharacters>;

            if (shapedTextRuns is null)
            {
                return null;
            }

            var runIndex = 0;
            var currentWidth = 0.0;
            var collapsedLength = 0;
            var textRange = textLine.TextRange;
            var shapedSymbol = TextFormatterImpl.CreateSymbol(properties.Symbol, FlowDirection.LeftToRight);

            if (properties.Width < shapedSymbol.GlyphRun.Size.Width)
            {
                return new List<ShapedTextCharacters>(0);
            }

            var availableWidth = properties.Width - shapedSymbol.Size.Width;

            while (runIndex < shapedTextRuns.Count)
            {
                var currentRun = shapedTextRuns[runIndex];

                currentWidth += currentRun.Size.Width;

                if (currentWidth > availableWidth)
                {
                    if (currentRun.TryMeasureCharacters(availableWidth, out var measuredLength))
                    {
                        if (isWordEllipsis && measuredLength < textRange.End)
                        {
                            var currentBreakPosition = 0;

                            var lineBreaker = new LineBreakEnumerator(currentRun.Text);

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

                    var shapedTextCharacters = new List<ShapedTextCharacters>(shapedTextRuns.Count);

                    if (collapsedLength > 0)
                    {
                        var splitResult = TextFormatterImpl.SplitShapedRuns(shapedTextRuns, collapsedLength);

                        shapedTextCharacters.AddRange(splitResult.First);

                        TextLineImpl.SortRuns(shapedTextCharacters);
                    }

                    shapedTextCharacters.Add(shapedSymbol);

                    return shapedTextCharacters;
                }

                availableWidth -= currentRun.Size.Width;

                collapsedLength += currentRun.GlyphRun.Characters.Length;

                runIndex++;
            }

            return null;
        }
    }
}
