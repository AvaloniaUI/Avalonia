// ReSharper disable ForCanBeConvertedToForeach
using System;
using System.Collections.Generic;
using System.Linq;

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
        public TextLeadingPrefixCharacterEllipsis(
            string ellipsis,
            int prefixLength,
            double width,
            TextRunProperties textRunProperties)
        {
            if (_prefixLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(prefixLength));
            }

            _prefixLength = prefixLength;
            Width = width;
            Symbol = new TextCharacters(ellipsis, textRunProperties);
        }

        /// <inheritdoc/>
        public override double Width { get; }

        /// <inheritdoc/>
        public override TextRun Symbol { get; }

        /// <inheritdoc />
        public override TextRun[]? Collapse(TextLine textLine)
        {
            var textRuns = textLine.TextRuns;

            if (textRuns.Count == 0)
            {
                return null;
            }

            var runIndex = 0;
            var currentWidth = 0.0;
            var shapedSymbol = TextFormatterImpl.CreateSymbol(Symbol, FlowDirection.LeftToRight);

            if (Width < shapedSymbol.GlyphRun.Size.Width)
            {
                return Array.Empty<TextRun>();
            }

            // Overview of ellipsis structure
            // Prefix length run | Ellipsis symbol | Post split run growing from the end |
            var availableWidth = Width - shapedSymbol.Size.Width;

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
                                shapedRun.TryMeasureCharacters(availableWidth, out var measuredLength);

                                if (measuredLength > 0)
                                {
                                    var collapsedRuns = new List<TextRun>(textRuns.Count + 1);

                                    // perf note: the runs are very likely to come from TextLineImpl,
                                    // which already uses an array: ToArray() won't ever be called in this case
                                    var textRunArray = textRuns as TextRun[] ?? textRuns.ToArray();

                                    IReadOnlyList<TextRun>? preSplitRuns;
                                    IReadOnlyList<TextRun>? postSplitRuns;

                                    if (_prefixLength > 0)
                                    {
                                        (preSplitRuns, postSplitRuns) = TextFormatterImpl.SplitTextRuns(
                                            textRunArray, Math.Min(_prefixLength, measuredLength));

                                        for (var i = 0; i < preSplitRuns.Count; i++)
                                        {
                                            var preSplitRun = preSplitRuns[i];
                                            collapsedRuns.Add(preSplitRun);
                                        }
                                    }
                                    else
                                    {
                                        preSplitRuns = null;
                                        postSplitRuns = textRunArray;
                                    }

                                    collapsedRuns.Add(shapedSymbol);

                                    if (measuredLength <= _prefixLength || postSplitRuns is null)
                                    {
                                        return collapsedRuns.ToArray();
                                    }

                                    var availableSuffixWidth = availableWidth;

                                    if (preSplitRuns is not null)
                                    {
                                        for (var i = 0; i < preSplitRuns.Count; i++)
                                        {
                                            var run = preSplitRuns[i];
                                            if (run is DrawableTextRun drawableTextRun)
                                            {
                                                availableSuffixWidth -= drawableTextRun.Size.Width;
                                            }
                                        }
                                    }

                                    for (var i = postSplitRuns.Count - 1; i >= 0; i--)
                                    {
                                        var run = postSplitRuns[i];

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

                                                            collapsedRuns.Add(splitSuffix.Second!);
                                                        }
                                                    }

                                                    break;
                                                }
                                        }
                                    }

                                    return collapsedRuns.ToArray();
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

                runIndex++;
            }

            return null;
        }
    }
}
