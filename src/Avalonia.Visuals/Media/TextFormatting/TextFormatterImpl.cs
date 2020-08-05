using System;
using System.Collections.Generic;
using Avalonia.Media.TextFormatting.Unicode;

namespace Avalonia.Media.TextFormatting
{
    internal class TextFormatterImpl : TextFormatter
    {
        /// <inheritdoc cref="TextFormatter.FormatLine"/>
        public override TextLine FormatLine(ITextSource textSource, int firstTextSourceIndex, double paragraphWidth,
            TextParagraphProperties paragraphProperties, TextLineBreak previousLineBreak = null)
        {
            var textWrapping = paragraphProperties.TextWrapping;

            var textRuns = FetchTextRuns(textSource, firstTextSourceIndex, previousLineBreak, out var nextLineBreak);

            var textRange = GetTextRange(textRuns);

            TextLine textLine;

            switch (textWrapping)
            {
                case TextWrapping.NoWrap:
                    {
                        var textLineMetrics =
                            TextLineMetrics.Create(textRuns, textRange, paragraphWidth, paragraphProperties);

                        textLine = new TextLineImpl(textRuns, textLineMetrics, nextLineBreak);
                        break;
                    }
                case TextWrapping.WrapWithOverflow:
                case TextWrapping.Wrap:
                    {
                        textLine = PerformTextWrapping(textRuns, textRange, paragraphWidth, paragraphProperties,
                            nextLineBreak);
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return textLine;
        }

        /// <summary>
        /// Measures the number of characters that fits into available width.
        /// </summary>
        /// <param name="textCharacters">The text run.</param>
        /// <param name="availableWidth">The available width.</param>
        /// <returns></returns>
        internal static int MeasureCharacters(ShapedTextCharacters textCharacters, double availableWidth)
        {
            var glyphRun = textCharacters.GlyphRun;

            if (glyphRun.Bounds.Width < availableWidth)
            {
                return glyphRun.Characters.Length;
            }

            var glyphCount = 0;

            var currentWidth = 0.0;

            if (glyphRun.GlyphAdvances.IsEmpty)
            {
                var glyphTypeface = glyphRun.GlyphTypeface;

                for (var i = 0; i < glyphRun.GlyphClusters.Length; i++)
                {
                    var glyph = glyphRun.GlyphIndices[i];

                    var advance = glyphTypeface.GetGlyphAdvance(glyph) * glyphRun.Scale;

                    if (currentWidth + advance > availableWidth)
                    {
                        break;
                    }

                    currentWidth += advance;

                    glyphCount++;
                }
            }
            else
            {
                foreach (var advance in glyphRun.GlyphAdvances)
                {
                    if (currentWidth + advance > availableWidth)
                    {
                        break;
                    }

                    currentWidth += advance;

                    glyphCount++;
                }
            }

            if (glyphCount == glyphRun.GlyphIndices.Length)
            {
                return glyphRun.Characters.Length;
            }

            if (glyphRun.GlyphClusters.IsEmpty)
            {
                return glyphCount;
            }

            var firstCluster = glyphRun.GlyphClusters[0];

            var lastCluster = glyphRun.GlyphClusters[glyphCount];

            return lastCluster - firstCluster;
        }

        /// <summary>
        /// Split a sequence of runs into two segments at specified length.
        /// </summary>
        /// <param name="textRuns">The text run's.</param>
        /// <param name="length">The length to split at.</param>
        /// <returns>The split text runs.</returns>
        internal static SplitTextRunsResult SplitTextRuns(List<ShapedTextCharacters> textRuns, int length)
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

                var firstCount = currentRun.GlyphRun.Characters.Length >= 1 ? i + 1 : i;

                var first = new List<ShapedTextCharacters>(firstCount);

                if (firstCount > 1)
                {
                    for (var j = 0; j < i; j++)
                    {
                        first.Add(textRuns[j]);
                    }
                }

                var secondCount = textRuns.Count - firstCount;

                if (currentLength + currentRun.GlyphRun.Characters.Length == length)
                {
                    var second = new List<ShapedTextCharacters>(secondCount);

                    var offset = currentRun.GlyphRun.Characters.Length > 1 ? 1 : 0;

                    if (secondCount > 0)
                    {
                        for (var j = 0; j < secondCount; j++)
                        {
                            second.Add(textRuns[i + j + offset]);
                        }
                    }

                    first.Add(currentRun);

                    return new SplitTextRunsResult(first, second);
                }
                else
                {
                    secondCount++;

                    var second = new List<ShapedTextCharacters>(secondCount);

                    var split = currentRun.Split(length - currentLength);

                    first.Add(split.First);

                    second.Add(split.Second);

                    if (secondCount > 0)
                    {
                        for (var j = 1; j < secondCount; j++)
                        {
                            second.Add(textRuns[i + j]);
                        }
                    }

                    return new SplitTextRunsResult(first, second);
                }
            }

            return new SplitTextRunsResult(textRuns, null);
        }

        /// <summary>
        /// Fetches text runs.
        /// </summary>
        /// <param name="textSource">The text source.</param>
        /// <param name="firstTextSourceIndex">The first text source index.</param>
        /// <param name="previousLineBreak">Previous line break. Can be null.</param>
        /// <param name="nextLineBreak">Next line break. Can be null.</param>
        /// <returns>
        /// The formatted text runs.
        /// </returns>
        private static List<ShapedTextCharacters> FetchTextRuns(ITextSource textSource,
            int firstTextSourceIndex, TextLineBreak previousLineBreak, out TextLineBreak nextLineBreak)
        {
            nextLineBreak = default;

            var currentLength = 0;

            var textRuns = new List<ShapedTextCharacters>();

            if (previousLineBreak != null)
            {
                for (var index = 0; index < previousLineBreak.RemainingCharacters.Count; index++)
                {
                    var shapedCharacters = previousLineBreak.RemainingCharacters[index];

                    if (shapedCharacters == null)
                    {
                        continue;
                    }

                    textRuns.Add(shapedCharacters);

                    if (TryGetLineBreak(shapedCharacters, out var runLineBreak))
                    {
                        var splitResult = SplitTextRuns(textRuns, currentLength + runLineBreak.PositionWrap);

                        if (++index < previousLineBreak.RemainingCharacters.Count)
                        {
                            for (; index < previousLineBreak.RemainingCharacters.Count; index++)
                            {
                                splitResult.Second.Add(previousLineBreak.RemainingCharacters[index]);
                            }
                        }

                        nextLineBreak = new TextLineBreak(splitResult.Second);

                        return splitResult.First;
                    }

                    currentLength += shapedCharacters.Text.Length;
                }
            }

            firstTextSourceIndex += currentLength;

            var textRunEnumerator = new TextRunEnumerator(textSource, firstTextSourceIndex);

            while (textRunEnumerator.MoveNext())
            {
                var textRun = textRunEnumerator.Current;

                switch (textRun)
                {
                    case TextCharacters textCharacters:
                        {
                            var shapeableRuns = textCharacters.GetShapeableCharacters();

                            foreach (var run in shapeableRuns)
                            {
                                var glyphRun = TextShaper.Current.ShapeText(run.Text, run.Properties.Typeface,
                                    run.Properties.FontRenderingEmSize, run.Properties.CultureInfo);

                                var shapedCharacters = new ShapedTextCharacters(glyphRun, run.Properties);

                                textRuns.Add(shapedCharacters);
                            }

                            break;
                        }
                }

                if (TryGetLineBreak(textRun, out var runLineBreak))
                {
                    var splitResult = SplitTextRuns(textRuns, currentLength + runLineBreak.PositionWrap);

                    nextLineBreak = new TextLineBreak(splitResult.Second);

                    return splitResult.First;
                }

                currentLength += textRun.Text.Length;
            }

            return textRuns;
        }

        private static bool TryGetLineBreak(TextRun textRun, out LineBreak lineBreak)
        {
            lineBreak = default;

            if (textRun.Text.IsEmpty)
            {
                return false;
            }

            var lineBreakEnumerator = new LineBreakEnumerator(textRun.Text);

            while (lineBreakEnumerator.MoveNext())
            {
                if (!lineBreakEnumerator.Current.Required)
                {
                    continue;
                }

                lineBreak = lineBreakEnumerator.Current;

                if (lineBreak.PositionWrap >= textRun.Text.Length)
                {
                    return true;
                }

                //The line breaker isn't treating \n\r as a pair so we have to fix that here.
                if (textRun.Text[lineBreak.PositionMeasure] == '\n'
                    && textRun.Text[lineBreak.PositionWrap] == '\r')
                {
                    lineBreak = new LineBreak(lineBreak.PositionMeasure, lineBreak.PositionWrap + 1,
                        lineBreak.Required);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Performs text wrapping returns a list of text lines.
        /// </summary>
        /// <param name="textRuns">The text run's.</param>
        /// <param name="textRange">The text range that is covered by the text runs.</param>
        /// <param name="paragraphWidth">The paragraph width.</param>
        /// <param name="paragraphProperties">The text paragraph properties.</param>
        /// <param name="currentLineBreak">The current line break if the line was explicitly broken.</param>
        /// <returns>The wrapped text line.</returns>
        private static TextLine PerformTextWrapping(List<ShapedTextCharacters> textRuns, TextRange textRange,
            double paragraphWidth, TextParagraphProperties paragraphProperties, TextLineBreak currentLineBreak)
        {
            var availableWidth = paragraphWidth;
            var currentWidth = 0.0;
            var runIndex = 0;
            var currentLength = 0;

            while (runIndex < textRuns.Count)
            {
                var currentRun = textRuns[runIndex];

                if (currentWidth + currentRun.GlyphRun.Bounds.Width > availableWidth)
                {
                    var measuredLength = MeasureCharacters(currentRun, paragraphWidth - currentWidth);

                    var breakFound = false;

                    var currentBreakPosition = 0;

                    if (measuredLength < currentRun.Text.Length)
                    {
                        var lineBreaker = new LineBreakEnumerator(currentRun.Text);

                        while (currentBreakPosition < measuredLength && lineBreaker.MoveNext())
                        {
                            var nextBreakPosition = lineBreaker.Current.PositionWrap;

                            if (nextBreakPosition == 0 || nextBreakPosition > measuredLength)
                            {
                                break;
                            }

                            breakFound = lineBreaker.Current.Required ||
                                         lineBreaker.Current.PositionWrap != currentRun.Text.Length;

                            currentBreakPosition = nextBreakPosition;
                        }
                    }

                    if (breakFound)
                    {
                        measuredLength = currentBreakPosition;
                    }
                    else
                    {
                        if (paragraphProperties.TextWrapping == TextWrapping.WrapWithOverflow)
                        {
                            var lineBreaker = new LineBreakEnumerator(currentRun.Text.Skip(currentBreakPosition));

                            if (lineBreaker.MoveNext())
                            {
                                measuredLength = currentBreakPosition + lineBreaker.Current.PositionWrap;
                            }
                        }
                    }

                    currentLength += measuredLength;

                    var splitResult = SplitTextRuns(textRuns, currentLength);

                    var textLineMetrics = TextLineMetrics.Create(splitResult.First,
                        new TextRange(textRange.Start, currentLength), paragraphWidth, paragraphProperties);

                    var remainingCharacters = splitResult.Second;

                    if (currentLineBreak?.RemainingCharacters != null)
                    {
                        if (remainingCharacters != null)
                        {
                            remainingCharacters.AddRange(currentLineBreak.RemainingCharacters);
                        }
                        else
                        {
                            remainingCharacters = new List<ShapedTextCharacters>(currentLineBreak.RemainingCharacters);
                        }
                    }

                    var lineBreak = remainingCharacters != null && remainingCharacters.Count > 0 ?
                        new TextLineBreak(remainingCharacters) :
                        null;

                    return new TextLineImpl(splitResult.First, textLineMetrics, lineBreak);
                }

                currentWidth += currentRun.GlyphRun.Bounds.Width;

                currentLength += currentRun.GlyphRun.Characters.Length;

                runIndex++;
            }

            return new TextLineImpl(textRuns,
                TextLineMetrics.Create(textRuns, textRange, paragraphWidth, paragraphProperties),
                currentLineBreak?.RemainingCharacters != null ?
                    new TextLineBreak(currentLineBreak.RemainingCharacters) :
                    null);
        }

        /// <summary>
        /// Gets the text range that is covered by the text runs.
        /// </summary>
        /// <param name="textRuns">The text runs.</param>
        /// <returns>The text range that is covered by the text runs.</returns>
        private static TextRange GetTextRange(IReadOnlyList<TextRun> textRuns)
        {
            if (textRuns is null || textRuns.Count == 0)
            {
                return new TextRange();
            }

            var firstTextRun = textRuns[0];

            if (textRuns.Count == 1)
            {
                return new TextRange(firstTextRun.Text.Start, firstTextRun.Text.Length);
            }

            var start = firstTextRun.Text.Start;

            var end = textRuns[textRuns.Count - 1].Text.End + 1;

            return new TextRange(start, end - start);
        }

        internal readonly struct SplitTextRunsResult
        {
            public SplitTextRunsResult(List<ShapedTextCharacters> first, List<ShapedTextCharacters> second)
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
            public List<ShapedTextCharacters> First { get; }

            /// <summary>
            /// Gets the second text runs.
            /// </summary>
            /// <value>
            /// The second text runs.
            /// </value>
            public List<ShapedTextCharacters> Second { get; }
        }

        private struct TextRunEnumerator
        {
            private readonly ITextSource _textSource;
            private int _pos;

            public TextRunEnumerator(ITextSource textSource, int firstTextSourceIndex)
            {
                _textSource = textSource;
                _pos = firstTextSourceIndex;
                Current = null;
            }

            // ReSharper disable once MemberHidesStaticFromOuterClass
            public TextRun Current { get; private set; }

            public bool MoveNext()
            {
                Current = _textSource.GetTextRun(_pos);

                if (Current is null)
                {
                    return false;
                }

                if (Current.TextSourceLength == 0)
                {
                    return false;
                }

                _pos += Current.TextSourceLength;

                return !(Current is TextEndOfLine);
            }
        }
    }
}
