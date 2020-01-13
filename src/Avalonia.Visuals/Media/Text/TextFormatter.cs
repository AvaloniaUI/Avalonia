using System;
using System.Collections.Generic;
using Avalonia.Media.Text.Unicode;
using Avalonia.Platform;
using Avalonia.Utility;

namespace Avalonia.Media.Text
{
    public static class TextFormatter
    {
        private static readonly ReadOnlySlice<char> s_ellipsis = new ReadOnlySlice<char>(new[] { '\u2026' });

        /// <summary>
        ///     Creates an ellipsis.
        /// </summary>
        /// <param name="textFormat">The text format.</param>
        /// <param name="foreground">The foreground.</param>
        /// <returns></returns>
        private static TextRun CreateEllipsisRun(TextFormat textFormat, IBrush foreground)
        {
            var formatterImpl = AvaloniaLocator.Current.GetService<ITextFormatterImpl>();

            var glyphRun = formatterImpl.CreateShapedGlyphRun(s_ellipsis, textFormat);

            return new TextRun(glyphRun, textFormat, foreground);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="paragraphWidth"></param>
        /// <param name="paragraphProperties"></param>
        /// <param name="textStyleRuns"></param>
        /// <returns></returns>
        public static TextLine FormatLine(in ReadOnlySlice<char> text, double paragraphWidth,
            TextParagraphProperties paragraphProperties, ReadOnlySpan<TextStyleRun> textStyleRuns = default)
        {
            var textTrimming = paragraphProperties.TextTrimming;
            var textWrapping = paragraphProperties.TextWrapping;
            TextLine textLine;

            var textRuns = FormatTextRuns(text, paragraphProperties.DefaultTextStyle, textStyleRuns);

            if (textTrimming != TextTrimming.None)
            {
                textLine = PerformTextTrimming(text, paragraphWidth, paragraphProperties, textRuns);
            }
            else
            {
                if (textWrapping == TextWrapping.Wrap)
                {
                    textLine = PerformTextWrapping(text, paragraphWidth, paragraphProperties, textRuns);
                }
                else
                {
                    var textLineMetrics =
                        TextLineMetrics.Create(textRuns, paragraphWidth, paragraphProperties.TextAlignment);

                    textLine = new TextLine(text, textRuns, textLineMetrics);
                }
            }

            return textLine;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="defaultTextStyle"></param>
        /// <param name="textStyleOverrides"></param>
        /// <returns></returns>
        public static List<TextRun> FormatTextRuns(ReadOnlySlice<char> text, TextStyle defaultTextStyle, ReadOnlySpan<TextStyleRun> textStyleOverrides = default)
        {
            var formatterImpl = AvaloniaLocator.Current.GetService<ITextFormatterImpl>();

            var textRuns = new List<TextRun>();

            while (text.Length > 0)
            {
                var shapableTextStyleRun = CreateTextStyleRunWithOverride(text, defaultTextStyle, ref textStyleOverrides, true);

                var runText = text.Take(shapableTextStyleRun.TextPointer.Length);

                shapableTextStyleRun =
                    formatterImpl.CreateShapableTextStyleRun(runText, defaultTextStyle);

                runText = runText.Take(shapableTextStyleRun.TextPointer.Length);

                var currentGlyphRun = formatterImpl.CreateShapedGlyphRun(runText, shapableTextStyleRun.Style.TextFormat);

                while (runText.Start <= shapableTextStyleRun.TextPointer.End)
                {
                    var currentTextStyleRun = CreateTextStyleRunWithOverride(runText, defaultTextStyle, ref textStyleOverrides);

                    var textPointer = currentTextStyleRun.TextPointer;

                    var textFormat = currentTextStyleRun.Style.TextFormat;

                    var foreground = currentTextStyleRun.Style.Foreground;

                    if (currentTextStyleRun.TextPointer.End != runText.End)
                    {
                        var split = SplitGlyphRun(currentGlyphRun, textPointer.Length);

                        textRuns.Add(new TextRun(split.First, textFormat, foreground));

                        currentGlyphRun = split.Second;
                    }
                    else
                    {
                        textRuns.Add(new TextRun(currentGlyphRun, textFormat, foreground));
                    }

                    runText = runText.Skip(currentTextStyleRun.TextPointer.Length);
                }

                text = text.Skip(shapableTextStyleRun.TextPointer.Length);
            }

            return textRuns;
        }

        public static TextStyleRun CreateTextStyleRunWithOverride(ReadOnlySlice<char> text, TextStyle defaultTextStyle,
            ref ReadOnlySpan<TextStyleRun> textStyleOverrides, bool optimizeForShaping = false)
        {
            var currentTextStyle = defaultTextStyle;

            var hasOverride = false;

            var i = 0;

            var length = 0;

            for (; i < textStyleOverrides.Length; i++)
            {
                var styleOverride = textStyleOverrides[i];

                var textPointer = styleOverride.TextPointer;

                if (textPointer.End < text.Start)
                {
                    continue;
                }

                if (textPointer.Start > text.End)
                {
                    break;
                }

                if (textPointer.Start > text.Start)
                {
                    if (styleOverride.Style.TextFormat != currentTextStyle.TextFormat || !optimizeForShaping &&
                        styleOverride.Style.Foreground != currentTextStyle.Foreground)
                    {
                        length = Math.Min(Math.Abs(textPointer.Start - text.Start), text.Length);

                        break;
                    }
                }

                length += Math.Min(text.Length - length, textPointer.Length);

                if (!optimizeForShaping && text.Start + length == textPointer.End + 1)
                {
                    textStyleOverrides = textStyleOverrides.Slice(i + 1);
                }

                if (hasOverride)
                {
                    continue;
                }

                hasOverride = true;

                currentTextStyle = styleOverride.Style;
            }

            if (length < text.Length && i == textStyleOverrides.Length)
            {
                if (optimizeForShaping && currentTextStyle.TextFormat == defaultTextStyle.TextFormat ||
                    !optimizeForShaping && currentTextStyle.Foreground == defaultTextStyle.Foreground &&
                    currentTextStyle.TextFormat == defaultTextStyle.TextFormat)
                {
                    length = text.Length;
                }
            }

            if (length != text.Length)
            {
                text = text.Take(length);
            }

            return new TextStyleRun(new TextPointer(text.Start, length), currentTextStyle);
        }

        /// <summary>
        ///     Performs text trimming and returns a trimmed line.
        /// </summary>
        /// <param name="paragraphWidth"></param>
        /// <param name="paragraphProperties"></param>
        /// <param name="textRuns"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static TextLine PerformTextTrimming(in ReadOnlySlice<char> text, double paragraphWidth,
            TextParagraphProperties paragraphProperties, IReadOnlyList<TextRun> textRuns)
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
                    var ellipsisRun = CreateEllipsisRun(currentRun.TextFormat, currentRun.Foreground);

                    var measuredLength = MeasureText(textRuns, paragraphWidth - ellipsisRun.GlyphRun.Bounds.Width);

                    if (textTrimming == TextTrimming.WordEllipsis)
                    {
                        if (measuredLength < text.End)
                        {
                            var currentBreakPosition = 0;

                            var lineBreaker = new LineBreaker(text);

                            while (currentBreakPosition < measuredLength && lineBreaker.NextBreak())
                            {
                                var nextBreakPosition = lineBreaker.CurrentBreak.PositionWrap;

                                if (nextBreakPosition == -1)
                                {
                                    break;
                                }

                                if (nextBreakPosition > measuredLength)
                                {
                                    break;
                                }

                                currentBreakPosition = nextBreakPosition;
                            }

                            measuredLength = currentBreakPosition + 1;
                        }
                    }

                    var splitResult = SplitTextRuns(textRuns, measuredLength);

                    var trimmedRuns = new List<TextRun>(splitResult.First.Count + 1);

                    trimmedRuns.AddRange(splitResult.First);

                    trimmedRuns.Add(ellipsisRun);

                    var textLineMetrics =
                        TextLineMetrics.Create(trimmedRuns, paragraphWidth, paragraphProperties.TextAlignment);

                    return new TextLine(text.Take(measuredLength), textRuns, textLineMetrics);
                }

                availableWidth -= currentRun.GlyphRun.Bounds.Width;

                runIndex++;
            }

            return new TextLine(text, textRuns,
                TextLineMetrics.Create(textRuns, paragraphWidth, paragraphProperties.TextAlignment));
        }

        /// <summary>
        ///     Performs text wrapping returns a list of text lines.
        /// </summary>
        /// <param name="paragraphProperties"></param>
        /// <param name="textRuns">The text run'S.</param>
        /// <param name="text">The text to analyze for break opportunities.</param>
        /// <param name="paragraphWidth"></param>
        /// <returns></returns>
        public static TextLine PerformTextWrapping(in ReadOnlySlice<char> text, double paragraphWidth, TextParagraphProperties paragraphProperties, IReadOnlyList<TextRun> textRuns)
        {
            var currentPosition = text.Start;
            var currentTextRuns = textRuns;

            var count = MeasureText(currentTextRuns, paragraphWidth);

            var endPosition = currentPosition + count;

            if (endPosition < text.Start + text.Length)
            {
                var currentBreakPosition = currentPosition;

                var lineBreaker = new LineBreaker(text);

                while (currentBreakPosition < endPosition && lineBreaker.NextBreak())
                {
                    var nextBreakPosition = lineBreaker.CurrentBreak.PositionWrap;

                    if (nextBreakPosition == -1 || nextBreakPosition > endPosition)
                    {
                        break;
                    }

                    currentBreakPosition = nextBreakPosition;
                }

                var length = currentBreakPosition - currentPosition;

                if (length == 0)
                {
                    length = count;
                }

                var splitResult = SplitTextRuns(currentTextRuns, length);

                var textLineMetrics = TextLineMetrics.Create(splitResult.First, paragraphWidth, paragraphProperties.TextAlignment);

                return new TextLine(text.AsSlice(currentPosition, length), splitResult.First, textLineMetrics);
            }
            else
            {
                var textLineMetrics = TextLineMetrics.Create(currentTextRuns, paragraphWidth, paragraphProperties.TextAlignment);

                return new TextLine(text, currentTextRuns, textLineMetrics);
            }
        }

        /// <summary>
        ///     Measures the number of characters that fits into available width.
        /// </summary>
        /// <param name="textRuns">The text runs.</param>
        /// <param name="availableWidth">The available width.</param>
        /// <returns></returns>
        public static int MeasureText(IReadOnlyList<TextRun> textRuns, double availableWidth)
        {
            var measuredWidth = 0.0;
            var count = 0;

            for (var i = 0; measuredWidth < availableWidth && i < textRuns.Count; i++)
            {
                var textRun = textRuns[i];

                if (textRun.GlyphRun.Bounds.Width + measuredWidth > availableWidth)
                {
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

                    count += characterHit.FirstCharacterIndex - textRun.GlyphRun.Characters.Start +
                             (textRun.GlyphRun.IsLeftToRight ? characterHit.TrailingLength : 0);

                    break;
                }

                measuredWidth += textRun.GlyphRun.Bounds.Width;

                count += textRun.GlyphRun.Characters.Length;
            }

            return count;
        }

        public readonly struct SplitTextRunsResult
        {
            public SplitTextRunsResult(IReadOnlyList<TextRun> first, IReadOnlyList<TextRun> second)
            {
                First = first;

                Second = second;
            }

            /// <summary>
            ///     Gets the first text runs.
            /// </summary>
            /// <value>
            ///     The first text runs.
            /// </value>
            public IReadOnlyList<TextRun> First { get; }

            /// <summary>
            ///     Gets the second text runs.
            /// </summary>
            /// <value>
            ///     The second text runs.
            /// </value>
            public IReadOnlyList<TextRun> Second { get; }
        }

        /// <summary>
        ///     Split a sequence of runs into two segments at specified length.
        /// </summary>
        /// <param name="textRuns">The text run's.</param>
        /// <param name="length">The length to split at.</param>
        /// <returns></returns>
        public static SplitTextRunsResult SplitTextRuns(IReadOnlyList<TextRun> textRuns, int length)
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

                var first = new TextRun[firstCount];

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
                    var second = new TextRun[secondCount];

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

                    var second = new TextRun[secondCount];

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

        //public readonly struct SplitTextRunPropertiesResult
        //{
        //    public SplitTextRunPropertiesResult(List<TextStyleRun> first,
        //        List<TextStyleRun> second)
        //    {
        //        First = first;
        //        Second = second;
        //    }

        //    public List<TextStyleRun> First { get; }

        //    public List<TextStyleRun> Second { get; }
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="textRunProperties"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        //public static SplitTextRunPropertiesResult SplitTextRunProperties(List<TextStyleRun> textRunProperties, int length)
        //{
        //    var currentLength = 0;
        //    var runIndex = 0;

        //    while (runIndex < textRunProperties.Count)
        //    {
        //        var run = textRunProperties[runIndex];

        //        var textLength = run.TextPointer.Length;

        //        if (currentLength + textLength > length)
        //        {
        //            break;
        //        }

        //        currentLength += textLength;

        //        runIndex++;
        //    }

        //    if (currentLength == length && runIndex == textRunProperties.Count)
        //    {
        //        return new SplitTextRunPropertiesResult(textRunProperties, null);
        //    }

        //    var first = new List<TextStyleRun>(runIndex);

        //    for (var i = 0; i < runIndex; i++)
        //    {
        //        first.Add(textRunProperties[i]);
        //    }

        //    var split = textRunProperties[runIndex].Split(length - currentLength);

        //    first.Add(split.First);

        //    var second = new List<TextStyleRun>(textRunProperties.Count - runIndex) { split.Second };

        //    for (var i = 1; i < textRunProperties.Count - runIndex; i++)
        //    {
        //        second.Add(textRunProperties[i + runIndex]);
        //    }

        //    return new SplitTextRunPropertiesResult(first, second);
        //}

        private readonly struct SplitGlyphRunResult
        {
            public SplitGlyphRunResult(GlyphRun first, GlyphRun second)
            {
                First = first;

                Second = second;
            }

            public GlyphRun First { get; }

            public GlyphRun Second { get; }
        }

        private static SplitGlyphRunResult SplitGlyphRun(GlyphRun glyphRun, int textLength)
        {
            if (glyphRun.Characters.Length == textLength)
            {
                return new SplitGlyphRunResult(glyphRun, null);
            }

            var glyphCount = 0;

            for (var i = 0; i < textLength;)
            {
                CodepointReader.Read(glyphRun.Characters, ref i);

                glyphCount++;
            }

            if (glyphRun.GlyphIndices.Length == glyphCount)
            {
                return new SplitGlyphRunResult(glyphRun, null);
            }

            var firstGlyphRun = new GlyphRun(
                glyphRun.GlyphTypeface,
                glyphRun.FontRenderingEmSize,
                glyphRun.GlyphIndices.Take(glyphCount),
                glyphRun.GlyphAdvances.Take(glyphCount),
                glyphRun.GlyphOffsets.Take(glyphCount),
                glyphRun.Characters.Take(textLength),
                glyphRun.GlyphClusters.Take(textLength));

            var secondGlyphRun = new GlyphRun(
                glyphRun.GlyphTypeface,
                glyphRun.FontRenderingEmSize,
                glyphRun.GlyphIndices.Skip(glyphCount),
                glyphRun.GlyphAdvances.Skip(glyphCount),
                glyphRun.GlyphOffsets.Skip(glyphCount),
                glyphRun.Characters.Skip(textLength),
                glyphRun.GlyphClusters.Skip(textLength));

            return new SplitGlyphRunResult(firstGlyphRun, secondGlyphRun);
        }
    }
}
