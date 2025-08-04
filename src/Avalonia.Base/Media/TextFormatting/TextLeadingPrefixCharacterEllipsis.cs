// ReSharper disable ForCanBeConvertedToForeach
using System;
using System.Collections.Generic;
using static Avalonia.Media.TextFormatting.FormattingObjectPool;

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
        /// <param name="flowDirection">the flow direction of the collapes line.</param>
        public TextLeadingPrefixCharacterEllipsis(
            string ellipsis,
            int prefixLength,
            double width,
            TextRunProperties textRunProperties,
            FlowDirection flowDirection)
        {
            if (_prefixLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(prefixLength));
            }

            _prefixLength = prefixLength;
            Width = width;
            Symbol = new TextCharacters(ellipsis, textRunProperties);
            FlowDirection = flowDirection;
        }

        /// <inheritdoc/>
        public override double Width { get; }

        /// <inheritdoc/>
        public override TextRun Symbol { get; }

        public override FlowDirection FlowDirection { get; }

        /// <inheritdoc />
        public override TextRun[]? Collapse(TextLine textLine)
        {
            var shapedSymbol = TextFormatter.CreateSymbol(Symbol, FlowDirection.LeftToRight);

            if (Width < shapedSymbol.GlyphRun.Bounds.Width)
            {
                return Array.Empty<TextRun>();
            }

            var textRunEnumerator = new LogicalTextRunEnumerator(textLine);

            var availableWidth = Width - shapedSymbol.Size.Width;

            while (textRunEnumerator.MoveNext(out var run))
            {
                if (run is DrawableTextRun drawableTextRun)
                {
                    availableWidth -= drawableTextRun.Size.Width;

                    if (availableWidth < 0)
                    {
                        return CollapseWithPrefix(textLine, shapedSymbol);
                    }
                }
            }

            return null;
        }

        // Overview of ellipsis structure
        // Prefix length run | Ellipsis symbol | Post split run growing from the end |
        private TextRun[]? CollapseWithPrefix(
                TextLine textLine,
                ShapedTextRun shapedSymbol
            )
        {
            var objectPool = FormattingObjectPool.Instance;

            // meaningful order when this line contains RTL / reversed textRuns
            var textRunEnumerator = new LogicalTextRunEnumerator(textLine);
            var textRuns = objectPool.TextRunLists.Rent();
            while (textRunEnumerator.MoveNext(out var run))
            {
                textRuns.Add(run);
            }

            var collapsedRuns = objectPool.TextRunLists.Rent();

            RentedList<TextRun>? rentedPreSplitRuns = null;
            RentedList<TextRun>? rentedPostSplitRuns = null;

            try
            {
                IReadOnlyList<TextRun>? effectivePostSplitRuns;

                var availableSuffixWidth = Width - shapedSymbol.Size.Width;

                // prepare the prefix
                if (_prefixLength > 0)
                {
                    (rentedPreSplitRuns, rentedPostSplitRuns) = TextFormatterImpl.SplitTextRuns(textRuns, _prefixLength, objectPool);

                    effectivePostSplitRuns = rentedPostSplitRuns;

                    foreach (var preSplitRun in rentedPreSplitRuns)
                    {
                        collapsedRuns.Add(preSplitRun);
                        if (preSplitRun is DrawableTextRun drawableTextRun)
                        {
                            availableSuffixWidth -= drawableTextRun.Size.Width;
                        }
                    }
                }
                else
                {
                    effectivePostSplitRuns = textRuns;
                }

                // add Ellipsis symbol
                collapsedRuns.Add(shapedSymbol);

                if (effectivePostSplitRuns is null || availableSuffixWidth <= 0)
                {
                    return collapsedRuns.ToArray();
                }

                int suffixStartIndex = collapsedRuns.Count;

                // append the suffix backwards until it gets trimmed
                for (var i = effectivePostSplitRuns.Count - 1; i >= 0; i--)
                {
                    var run = effectivePostSplitRuns[i];

                    if (run is ShapedTextRun endShapedRun)
                    {
                        if (endShapedRun.TryMeasureCharactersBackwards(availableSuffixWidth,
                                out var suffixCount, out var suffixWidth))
                        {
                            if (endShapedRun.IsReversed)
                            {
                                endShapedRun.Reverse();
                            }

                            availableSuffixWidth -= suffixWidth;

                            if (suffixCount >= run.Length)
                            {
                                collapsedRuns.Insert(suffixStartIndex, run);
                            }
                            else
                            {
                                var splitSuffix = endShapedRun.Split(run.Length - suffixCount);
                                    collapsedRuns.Insert(suffixStartIndex, splitSuffix.Second!);
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    else if (run is DrawableTextRun drawableTextRun)
                    {
                        availableSuffixWidth -= drawableTextRun.Size.Width;

                        // entire run must fit
                        if (availableSuffixWidth >= 0)
                        {
                            collapsedRuns.Insert(suffixStartIndex, run);
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                return collapsedRuns.ToArray();
            }
            finally
            {
                objectPool.TextRunLists.Return(ref rentedPreSplitRuns);
                objectPool.TextRunLists.Return(ref rentedPostSplitRuns);
                objectPool.TextRunLists.Return(ref collapsedRuns);
                objectPool.TextRunLists.Return(ref textRuns);
            }
        }
    }
}
