using System;
using System.Collections.Generic;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Platform;
using Avalonia.Utility;

namespace Avalonia.Media.TextFormatting
{
    internal class SimpleTextFormatter : TextFormatter
    {
        private static readonly ReadOnlySlice<char> s_ellipsis = new ReadOnlySlice<char>(new[] { '\u2026' });

        /// <summary>
        /// Formats a text line.
        /// </summary>
        /// <param name="textSource">The text source.</param>
        /// <param name="firstTextSourceIndex">The first character index to start the text line from.</param>
        /// <param name="paragraphWidth">A <see cref="double"/> value that specifies the width of the paragraph that the line fills.</param>
        /// <param name="paragraphProperties">A <see cref="TextParagraphProperties"/> value that represents paragraph properties,
        /// such as TextWrapping, TextAlignment, or TextStyle.</param>
        /// <returns>The formatted line.</returns>
        public override TextLine FormatLine(ITextSource textSource, int firstTextSourceIndex, double paragraphWidth,
            TextParagraphProperties paragraphProperties)
        {
            var textTrimming = paragraphProperties.TextTrimming;
            var textWrapping = paragraphProperties.TextWrapping;
            TextLine textLine;

            var textRuns = FormatTextRuns(textSource, firstTextSourceIndex, out var textPointer);

            if (textTrimming != TextTrimming.None)
            {
                textLine = PerformTextTrimming(textPointer, textRuns, paragraphWidth, paragraphProperties);
            }
            else
            {
                if (textWrapping == TextWrapping.Wrap)
                {
                    textLine = PerformTextWrapping(textPointer, textRuns, paragraphWidth, paragraphProperties);
                }
                else
                {
                    var textLineMetrics =
                        TextLineMetrics.Create(textRuns, paragraphWidth, paragraphProperties.TextAlignment);

                    textLine = new SimpleTextLine(textPointer, textRuns, textLineMetrics);
                }
            }

            return textLine;
        }

        /// <summary>
        /// Formats text runs with optional text style overrides.
        /// </summary>
        /// <param name="textSource">The text source.</param>
        /// <param name="firstTextSourceIndex">The first text source index.</param>
        /// <param name="textPointer">The text pointer that covers the formatted text runs.</param>
        /// <returns>
        /// The formatted text runs.
        /// </returns>
        private List<ShapedTextRun> FormatTextRuns(ITextSource textSource, int firstTextSourceIndex, out TextPointer textPointer)
        {
            var start = firstTextSourceIndex;

            var textRuns = new List<ShapedTextRun>();

            while (true)
            {
                var textRun = textSource.GetTextRun(firstTextSourceIndex);

                if (textRun.Text.IsEmpty)
                {
                    break;
                }

                if (textRun is TextEndOfLine)
                {
                    break;
                }

                if (!(textRun is TextCharacters))
                {
                    throw new NotSupportedException("Run type not supported by the formatter.");
                }

                var runText = textRun.Text;

                while (!runText.IsEmpty)
                {
                    var shapableTextStyleRun = CreateShapableTextStyleRun(runText, textRun.Style);

                    var shapedRun = new ShapedTextRun(runText.Take(shapableTextStyleRun.TextPointer.Length),
                        shapableTextStyleRun.Style);

                    textRuns.Add(shapedRun);

                    runText = runText.Skip(shapedRun.Text.Length);
                }

                firstTextSourceIndex += textRun.Text.Length;
            }

            textPointer = new TextPointer(start, firstTextSourceIndex - start);

            return textRuns;
        }

        /// <summary>
        /// Performs text trimming and returns a trimmed line.
        /// </summary>
        /// <param name="paragraphWidth">A <see cref="double"/> value that specifies the width of the paragraph that the line fills.</param>
        /// <param name="paragraphProperties">A <see cref="TextParagraphProperties"/> value that represents paragraph properties,
        /// such as TextWrapping, TextAlignment, or TextStyle.</param>
        /// <param name="textRuns">The text runs to perform the trimming on.</param>
        /// <param name="text">The text that was used to construct the text runs.</param>
        /// <returns></returns>
        private TextLine PerformTextTrimming(TextPointer text, IReadOnlyList<ShapedTextRun> textRuns,
            double paragraphWidth, TextParagraphProperties paragraphProperties)
        {
            var textTrimming = paragraphProperties.TextTrimming;
            var availableWidth = paragraphWidth;
            var currentWidth = 0.0;
            var runIndex = 0;

            while (runIndex < textRuns.Count)
            {
                var currentRun = textRuns[runIndex];

                currentWidth += currentRun.GlyphRun.Bounds.Width;

                if (currentWidth > availableWidth)
                {
                    var ellipsisRun = CreateEllipsisRun(currentRun.Style);

                    var measuredLength = MeasureText(currentRun, availableWidth - ellipsisRun.GlyphRun.Bounds.Width);

                    if (textTrimming == TextTrimming.WordEllipsis)
                    {
                        if (measuredLength < text.End)
                        {
                            var currentBreakPosition = 0;

                            var lineBreaker = new LineBreakEnumerator(currentRun.Text);

                            while (currentBreakPosition < measuredLength && lineBreaker.MoveNext())
                            {
                                var nextBreakPosition = lineBreaker.Current.PositionWrap;

                                if (nextBreakPosition == 0)
                                {
                                    break;
                                }

                                if (nextBreakPosition > measuredLength)
                                {
                                    break;
                                }

                                currentBreakPosition = nextBreakPosition;
                            }

                            measuredLength = currentBreakPosition;
                        }
                    }

                    var splitResult = SplitTextRuns(textRuns, measuredLength);

                    var trimmedRuns = new List<ShapedTextRun>(splitResult.First.Count + 1);

                    trimmedRuns.AddRange(splitResult.First);

                    trimmedRuns.Add(ellipsisRun);

                    var textLineMetrics =
                        TextLineMetrics.Create(trimmedRuns, paragraphWidth, paragraphProperties.TextAlignment);

                    return new SimpleTextLine(text.Take(measuredLength), trimmedRuns, textLineMetrics);
                }

                availableWidth -= currentRun.GlyphRun.Bounds.Width;

                runIndex++;
            }

            return new SimpleTextLine(text, textRuns,
                TextLineMetrics.Create(textRuns, paragraphWidth, paragraphProperties.TextAlignment));
        }

        /// <summary>
        /// Performs text wrapping returns a list of text lines.
        /// </summary>
        /// <param name="paragraphProperties">The text paragraph properties.</param>
        /// <param name="textRuns">The text run'S.</param>
        /// <param name="text">The text to analyze for break opportunities.</param>
        /// <param name="paragraphWidth"></param>
        /// <returns></returns>
        private TextLine PerformTextWrapping(TextPointer text, IReadOnlyList<ShapedTextRun> textRuns,
            double paragraphWidth, TextParagraphProperties paragraphProperties)
        {
            var availableWidth = paragraphWidth;
            var currentWidth = 0.0;
            var runIndex = 0;

            while (runIndex < textRuns.Count)
            {
                var currentRun = textRuns[runIndex];

                currentWidth += currentRun.GlyphRun.Bounds.Width;

                if (currentWidth > availableWidth)
                {
                    var measuredLength = MeasureText(currentRun, paragraphWidth);

                    if (measuredLength < text.End)
                    {
                        var currentBreakPosition = -1;

                        var lineBreaker = new LineBreakEnumerator(currentRun.Text);

                        while (currentBreakPosition < measuredLength && lineBreaker.MoveNext())
                        {
                            var nextBreakPosition = lineBreaker.Current.PositionWrap;

                            if (nextBreakPosition == 0)
                            {
                                break;
                            }

                            if (nextBreakPosition > measuredLength)
                            {
                                break;
                            }

                            currentBreakPosition = nextBreakPosition;
                        }

                        if (currentBreakPosition != -1)
                        {
                            measuredLength = currentBreakPosition;
                        }
                    }

                    var splitResult = SplitTextRuns(textRuns, measuredLength);

                    var textLineMetrics =
                        TextLineMetrics.Create(splitResult.First, paragraphWidth, paragraphProperties.TextAlignment);

                    return new SimpleTextLine(text.Take(measuredLength), splitResult.First, textLineMetrics);
                }

                availableWidth -= currentRun.GlyphRun.Bounds.Width;

                runIndex++;
            }

            return new SimpleTextLine(text, textRuns,
                TextLineMetrics.Create(textRuns, paragraphWidth, paragraphProperties.TextAlignment));
        }

        /// <summary>
        /// Measures the number of characters that fits into available width.
        /// </summary>
        /// <param name="textRun">The text run.</param>
        /// <param name="availableWidth">The available width.</param>
        /// <returns></returns>
        private int MeasureText(ShapedTextRun textRun, double availableWidth)
        {
            if (textRun.GlyphRun.Bounds.Width < availableWidth)
            {
                return textRun.Text.Length;
            }

            var measuredWidth = 0.0;

            var index = 0;

            for (; index < textRun.GlyphRun.GlyphAdvances.Length; index++)
            {
                var advance = textRun.GlyphRun.GlyphAdvances[index];

                if (measuredWidth + advance > availableWidth)
                {
                    break;
                }

                measuredWidth += advance;
            }

            var cluster = textRun.GlyphRun.GlyphClusters[index];

            var characterHit = textRun.GlyphRun.FindNearestCharacterHit(cluster, out _);

            return characterHit.FirstCharacterIndex - textRun.GlyphRun.Characters.Start +
                   (textRun.GlyphRun.IsLeftToRight ? characterHit.TrailingLength : 0);
        }

        /// <summary>
        /// Creates an ellipsis.
        /// </summary>
        /// <param name="textStyle">The text style.</param>
        /// <returns></returns>
        private static ShapedTextRun CreateEllipsisRun(TextStyle textStyle)
        {
            var formatterImpl = AvaloniaLocator.Current.GetService<ITextShaperImpl>();

            var glyphRun = formatterImpl.ShapeText(s_ellipsis, textStyle.TextFormat);

            return new ShapedTextRun(glyphRun, textStyle);
        }

        private readonly struct SplitTextRunsResult
        {
            public SplitTextRunsResult(IReadOnlyList<ShapedTextRun> first, IReadOnlyList<ShapedTextRun> second)
            {
                First = first;

                Second = second;
            }

            /// <summary>
            /// Gets the first text runs.
            /// </summary>
            /// <value>
            /// The first text runs.
            /// </value>
            public IReadOnlyList<ShapedTextRun> First { get; }

            /// <summary>
            /// Gets the second text runs.
            /// </summary>
            /// <value>
            /// The second text runs.
            /// </value>
            public IReadOnlyList<ShapedTextRun> Second { get; }
        }

        /// <summary>
        /// Split a sequence of runs into two segments at specified length.
        /// </summary>
        /// <param name="textRuns">The text run's.</param>
        /// <param name="length">The length to split at.</param>
        /// <returns></returns>
        private static SplitTextRunsResult SplitTextRuns(IReadOnlyList<ShapedTextRun> textRuns, int length)
        {
            var currentLength = 0;

            for (var i = 0; i < textRuns.Count; i++)
            {
                var currentRun = textRuns[i];

                if (currentLength + currentRun.GlyphRun.Characters.Length < length)
                {
                    currentLength += currentRun.GlyphRun.Characters.Length;
                    continue;
                }

                var firstCount = currentRun.GlyphRun.Characters.Length > 1 ? i + 1 : i;

                var first = new ShapedTextRun[firstCount];

                if (firstCount > 1)
                {
                    for (var j = 0; j < i; j++)
                    {
                        first[j] = textRuns[j];
                    }
                }

                var secondCount = textRuns.Count - firstCount;

                if (currentLength + currentRun.GlyphRun.Characters.Length == length)
                {
                    var second = new ShapedTextRun[secondCount];

                    var offset = currentRun.GlyphRun.Characters.Length > 1 ? 1 : 0;

                    if (secondCount > 0)
                    {
                        for (var j = 0; j < secondCount; j++)
                        {
                            second[j] = textRuns[i + j + offset];
                        }
                    }

                    first[i] = currentRun;

                    return new SplitTextRunsResult(first, second);
                }
                else
                {
                    secondCount++;

                    var second = new ShapedTextRun[secondCount];

                    if (secondCount > 0)
                    {
                        for (var j = 1; j < secondCount; j++)
                        {
                            second[j] = textRuns[i + j];
                        }
                    }

                    var split = currentRun.Split(length - currentLength);

                    first[i] = split.First;

                    second[0] = split.Second;

                    return new SplitTextRunsResult(first, second);
                }
            }

            return new SplitTextRunsResult(textRuns, null);
        }
    }
}
