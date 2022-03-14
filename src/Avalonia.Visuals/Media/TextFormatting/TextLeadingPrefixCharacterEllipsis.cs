using System;
using System.Collections.Generic;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Ellipsis based on a fixed length leading prefix and suffix growing from the end.
    /// </summary>
    public class TextLeadingPrefixEllipsis : TextCollapsingProperties
    {
        private readonly int _prefixLength;

        /// <summary>
        /// Construct a text trailing word ellipsis collapsing properties
        /// </summary>
        /// <param name="width">width in which collapsing is constrained to</param>
        /// <param name="textRunProperties">text run properties of ellispis symbol</param>
        public TextLeadingPrefixEllipsis(
            ReadOnlySlice<char> ellipsis,
            int prefixLength,
            double width,
            TextRunProperties textRunProperties
        )
        {
            _prefixLength = prefixLength;
            Width = width;
            Symbol = new TextCharacters(ellipsis, textRunProperties);
        }

        /// <inheritdoc/>
        public sealed override double Width { get; }

        /// <inheritdoc/>
        public sealed override TextRun Symbol { get; }

        public override IReadOnlyList<TextRun>? Collapse(TextLine textLine)
        {
            var shapedTextRuns = textLine.TextRuns as List<ShapedTextCharacters>;

            if (shapedTextRuns is null)
            {
                return null;
            }

            var runIndex = 0;
            var currentWidth = 0.0;

            // TODO: From where this is supposed to come now?
            var flowDirection = FlowDirection.LeftToRight;

            var shapedSymbol = TextFormatterImpl.CreateSymbol(Symbol, flowDirection);

            if (Width < shapedSymbol.GlyphRun.Size.Width)
            {
                return new List<ShapedTextCharacters>(0);
            }

            var availableWidth = Width - shapedSymbol.Size.Width;

            while (runIndex < shapedTextRuns.Count)
            {
                var currentRun = shapedTextRuns[runIndex];

                currentWidth += currentRun.Size.Width;

                if (currentWidth > availableWidth)
                {
                    currentRun.TryMeasureCharacters(availableWidth, out var measuredLength);

                    var shapedTextCharacters = new List<ShapedTextCharacters>(shapedTextRuns.Count);

                    if (measuredLength > 0)
                    {
                        var splitResult = TextFormatterImpl.SplitShapedRuns(shapedTextRuns, Math.Min(_prefixLength, measuredLength));

                        shapedTextCharacters.AddRange(splitResult.First);

                        TextLineImpl.SortRuns(shapedTextCharacters);

                        shapedTextCharacters.Add(shapedSymbol);

                        if (measuredLength > _prefixLength && splitResult.Second is not null)
                        {
                            var availableSuffixWidth = availableWidth;

                            foreach (var run in splitResult.First)
                            {
                                availableSuffixWidth -= run.Size.Width;
                            }

                            for (int i = splitResult.Second.Count - 1; i >= 0; i--)
                            {
                                var run = splitResult.Second[i];

                                if (run.TryMeasureCharactersBackwards(availableSuffixWidth, out int suffixCount, out double suffixWidth))
                                {
                                    availableSuffixWidth -= suffixWidth;

                                    if (suffixCount > 0)
                                    {
                                        var splitSuffix = run.Split(run.TextSourceLength - suffixCount);

                                        shapedTextCharacters.Add(splitSuffix.Second!);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        shapedTextCharacters.Add(shapedSymbol);
                    }

                    return shapedTextCharacters;
                }

                availableWidth -= currentRun.Size.Width;

                runIndex++;
            }

            return null;
        }
    }
}
