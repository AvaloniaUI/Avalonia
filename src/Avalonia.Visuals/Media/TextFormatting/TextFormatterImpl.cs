using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting
{
    internal class TextFormatterImpl : TextFormatter
    {
        /// <inheritdoc cref="TextFormatter.FormatLine"/>
        public override TextLine FormatLine(ITextSource textSource, int firstTextSourceIndex, double paragraphWidth,
            TextParagraphProperties paragraphProperties, TextLineBreak? previousLineBreak = null)
        {
            var textWrapping = paragraphProperties.TextWrapping;
            FlowDirection flowDirection;
            TextLineBreak? nextLineBreak = null;
            List<ShapedTextCharacters> shapedRuns;

            var textRuns = FetchTextRuns(textSource, firstTextSourceIndex,
                out var textEndOfLine, out var textRange);

            if (previousLineBreak?.RemainingCharacters != null)
            {
                flowDirection = previousLineBreak.FlowDirection;
                shapedRuns = previousLineBreak.RemainingCharacters.ToList();
                nextLineBreak = previousLineBreak;
            }
            else
            {
                shapedRuns = ShapeTextRuns(textRuns, paragraphProperties.FlowDirection,out flowDirection);

                if(nextLineBreak == null && textEndOfLine != null)
                {
                    nextLineBreak = new TextLineBreak(textEndOfLine, flowDirection);
                }
            }

            TextLineImpl textLine;

            switch (textWrapping)
            {
                case TextWrapping.NoWrap:
                    {
                        TextLineImpl.SortRuns(shapedRuns);

                        textLine = new TextLineImpl(shapedRuns, textRange, paragraphWidth, paragraphProperties,
                        flowDirection, nextLineBreak);

                        textLine.FinalizeLine();

                        break;
                    }
                case TextWrapping.WrapWithOverflow:
                case TextWrapping.Wrap:
                    {
                        textLine = PerformTextWrapping(shapedRuns, textRange, paragraphWidth, paragraphProperties,
                            flowDirection, nextLineBreak);
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(textWrapping));
            }

            return textLine;
        }

        /// <summary>
        /// Split a sequence of runs into two segments at specified length.
        /// </summary>
        /// <param name="textRuns">The text run's.</param>
        /// <param name="length">The length to split at.</param>
        /// <returns>The split text runs.</returns>
        internal static SplitResult<List<ShapedTextCharacters>> SplitShapedRuns(List<ShapedTextCharacters> textRuns, int length)
        {
            var currentLength = 0;

            for (var i = 0; i < textRuns.Count; i++)
            {
                var currentRun = textRuns[i];

                if (currentLength + currentRun.Text.Length < length)
                {
                    currentLength += currentRun.Text.Length;
                    continue;
                }

                var firstCount = currentRun.Text.Length >= 1 ? i + 1 : i;

                var first = new List<ShapedTextCharacters>(firstCount);

                if (firstCount > 1)
                {
                    for (var j = 0; j < i; j++)
                    {
                        first.Add(textRuns[j]);
                    }
                }

                var secondCount = textRuns.Count - firstCount;

                if (currentLength + currentRun.Text.Length == length)
                {
                    var second = secondCount > 0 ? new List<ShapedTextCharacters>(secondCount) : null;

                    if (second != null)
                    {
                        var offset = currentRun.Text.Length >= 1 ? 1 : 0;

                        for (var j = 0; j < secondCount; j++)
                        {
                            second.Add(textRuns[i + j + offset]);
                        }
                    }

                    first.Add(currentRun);

                    return new SplitResult<List<ShapedTextCharacters>>(first, second);
                }
                else
                {
                    secondCount++;

                    var second = new List<ShapedTextCharacters>(secondCount);

                    var split = currentRun.Split(length - currentLength);

                    first.Add(split.First);

                    second.Add(split.Second!);

                    for (var j = 1; j < secondCount; j++)
                    {
                        second.Add(textRuns[i + j]);
                    }

                    return new SplitResult<List<ShapedTextCharacters>>(first, second);
                }
            }

            return new SplitResult<List<ShapedTextCharacters>>(textRuns, null);
        }

        /// <summary>
        /// Shape specified text runs with specified paragraph embedding.
        /// </summary>
        /// <param name="textRuns">The text runs to shape.</param>
        /// <param name="flowDirection">The paragraph embedding level.</param>
        /// <param name="resolvedFlowDirection">The resolved flow direction.</param>
        /// <returns>
        /// A list of shaped text characters.
        /// </returns>
        private static List<ShapedTextCharacters> ShapeTextRuns(List<TextCharacters> textRuns,
            FlowDirection flowDirection, out FlowDirection resolvedFlowDirection)
        {
            var shapedTextCharacters = new List<ShapedTextCharacters>();

            var biDiData = new BidiData((sbyte)flowDirection);

            foreach (var textRun in textRuns)
            {
                biDiData.Append(textRun.Text);
            }

            var biDi = BidiAlgorithm.Instance.Value!;

            biDi.Process(biDiData);

            var resolvedEmbeddingLevel = biDi.ResolveEmbeddingLevel(biDiData.Classes);

            resolvedFlowDirection =
                (resolvedEmbeddingLevel & 1) == 0 ? FlowDirection.LeftToRight : FlowDirection.RightToLeft;

            var shapeableRuns = new List<ShapeableTextCharacters>(textRuns.Count);

            foreach (var coalescedRuns in CoalesceLevels(textRuns, biDi.ResolvedLevels))
            {
                shapeableRuns.AddRange(coalescedRuns);
            }

            for (var index = 0; index < shapeableRuns.Count; index++)
            {
                var currentRun = shapeableRuns[index];
                var groupedRuns = new List<ShapeableTextCharacters>(2) { currentRun };
                var text = currentRun.Text;
                var start = currentRun.Text.Start;
                var length = currentRun.Text.Length;
                var bufferOffset = currentRun.Text.BufferOffset;

                while (index + 1 < shapeableRuns.Count)
                {
                    var nextRun = shapeableRuns[index + 1];

                    if (currentRun.CanShapeTogether(nextRun))
                    {
                        groupedRuns.Add(nextRun);

                        length += nextRun.Text.Length;
                        
                        if (start > nextRun.Text.Start)
                        {
                            start = nextRun.Text.Start;
                        }

                        if (bufferOffset > nextRun.Text.BufferOffset)
                        {
                            bufferOffset = nextRun.Text.BufferOffset;
                        }

                        text = new ReadOnlySlice<char>(text.Buffer, start, length, bufferOffset);
                        
                        index++;

                        currentRun = nextRun;

                        continue;
                    }

                    break;
                }

                shapedTextCharacters.AddRange(ShapeTogether(groupedRuns, text));
            }

            return shapedTextCharacters;
        }

        private static IReadOnlyList<ShapedTextCharacters> ShapeTogether(
            IReadOnlyList<ShapeableTextCharacters> textRuns, ReadOnlySlice<char> text)
        {
            var shapedRuns = new List<ShapedTextCharacters>(textRuns.Count);
            var firstRun = textRuns[0];

            var shapedBuffer = TextShaper.Current.ShapeText(text, firstRun.Properties.Typeface.GlyphTypeface,
                firstRun.Properties.FontRenderingEmSize, firstRun.Properties.CultureInfo, firstRun.BidiLevel);

            for (var i = 0; i < textRuns.Count; i++)
            {
                var currentRun = textRuns[i];

                var splitResult = shapedBuffer.Split(currentRun.Text.Length);

                shapedRuns.Add(new ShapedTextCharacters(splitResult.First, currentRun.Properties));

                shapedBuffer = splitResult.Second!;
            }

            return shapedRuns;
        }

        /// <summary>
        /// Coalesces ranges of the same bidi level to form <see cref="ShapeableTextCharacters"/>
        /// </summary>
        /// <param name="textCharacters">The text characters to form <see cref="ShapeableTextCharacters"/> from.</param>
        /// <param name="levels">The bidi levels.</param>
        /// <returns></returns>
        private static IEnumerable<IList<ShapeableTextCharacters>> CoalesceLevels(
            IReadOnlyList<TextCharacters> textCharacters,
            ReadOnlySlice<sbyte> levels)
        {
            if (levels.Length == 0)
            {
                yield break;
            }

            var levelIndex = 0;
            var runLevel = levels[0];

            TextRunProperties? previousProperties = null;
            TextCharacters? currentRun = null;
            var runText = ReadOnlySlice<char>.Empty;

            for (var i = 0; i < textCharacters.Count; i++)
            {
                var j = 0;
                currentRun = textCharacters[i];
                runText = currentRun.Text;

                for (; j < runText.Length;)
                {
                    Codepoint.ReadAt(runText, j, out var count);

                    if (levelIndex + 1 == levels.Length)
                    {
                        break;
                    }

                    levelIndex++;
                    j += count;

                    if (j == runText.Length)
                    {
                        yield return currentRun.GetShapeableCharacters(runText.Take(j), runLevel, ref previousProperties);

                        runLevel = levels[levelIndex];

                        continue;
                    }

                    if (levels[levelIndex] == runLevel)
                    {
                        continue;
                    }

                    // End of this run
                    yield return currentRun.GetShapeableCharacters(runText.Take(j), runLevel, ref previousProperties);

                    runText = runText.Skip(j);

                    j = 0;

                    // Move to next run
                    runLevel = levels[levelIndex];
                }
            }

            if (currentRun is null || runText.IsEmpty)
            {
                yield break;
            }

            yield return currentRun.GetShapeableCharacters(runText, runLevel, ref previousProperties);
        }

        /// <summary>
        /// Fetches text runs.
        /// </summary>
        /// <param name="textSource">The text source.</param>
        /// <param name="firstTextSourceIndex">The first text source index.</param>
        /// <param name="endOfLine"></param>
        /// <param name="textRange"></param>
        /// <returns>
        /// The formatted text runs.
        /// </returns>
        private static List<TextCharacters> FetchTextRuns(ITextSource textSource, int firstTextSourceIndex,
            out TextEndOfLine? endOfLine, out TextRange textRange)
        {
            var length = 0;

            endOfLine = null;

            var textRuns = new List<TextCharacters>();

            var textRunEnumerator = new TextRunEnumerator(textSource, firstTextSourceIndex);

            while (textRunEnumerator.MoveNext())
            {
                var textRun = textRunEnumerator.Current;

                if(textRun == null)
                {
                    break;
                }

                switch (textRun)
                {
                    case TextCharacters textCharacters:
                        {
                            if (TryGetLineBreak(textCharacters, out var runLineBreak))
                            {
                                var splitResult = new TextCharacters(textCharacters.Text.Take(runLineBreak.PositionWrap),
                                    textCharacters.Properties);

                                textRuns.Add(splitResult);

                                length += runLineBreak.PositionWrap;

                                textRange = new TextRange(firstTextSourceIndex, length);

                                return textRuns;
                            }

                            textRuns.Add(textCharacters);

                            break;
                        }
                    case TextEndOfLine textEndOfLine:
                        endOfLine = textEndOfLine;
                        break;
                }

                length += textRun.Text.Length;
            }

            textRange = new TextRange(firstTextSourceIndex, length);

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

                return lineBreak.PositionWrap >= textRun.Text.Length || true;
            }

            return false;
        }

        private static int MeasureLength(IReadOnlyList<ShapedTextCharacters> textRuns, TextRange textRange,
            double paragraphWidth)
        {
            var currentWidth = 0.0;
            var lastCluster = textRange.Start;

            foreach (var currentRun in textRuns)
            {
                for (var i = 0; i < currentRun.ShapedBuffer.Length; i++)
                {
                    var glyphInfo = currentRun.ShapedBuffer[i];

                    if (currentWidth + glyphInfo.GlyphAdvance > paragraphWidth)
                    {
                        var measuredLength = lastCluster - textRange.Start;

                        return measuredLength == 0 ? 1 : measuredLength;
                    }

                    lastCluster = glyphInfo.GlyphCluster;
                    currentWidth += glyphInfo.GlyphAdvance;
                }
            }

            return textRange.Length;
        }

        /// <summary>
        /// Performs text wrapping returns a list of text lines.
        /// </summary>
        /// <param name="textRuns"></param>
        /// <param name="textRange">The text range that is covered by the text runs.</param>
        /// <param name="paragraphWidth">The paragraph width.</param>
        /// <param name="paragraphProperties">The text paragraph properties.</param>
        /// <param name="flowDirection"></param>
        /// <param name="currentLineBreak">The current line break if the line was explicitly broken.</param>
        /// <returns>The wrapped text line.</returns>
        private static TextLineImpl PerformTextWrapping(List<ShapedTextCharacters> textRuns, TextRange textRange,
            double paragraphWidth, TextParagraphProperties paragraphProperties, FlowDirection flowDirection,
            TextLineBreak? currentLineBreak)
        {
            var measuredLength = MeasureLength(textRuns, textRange, paragraphWidth);

            var currentLength = 0;

            var lastWrapPosition = 0;

            var currentPosition = 0;

            for (var index = 0; index < textRuns.Count; index++)
            {
                var currentRun = textRuns[index];

                var lineBreaker = new LineBreakEnumerator(currentRun.Text);

                var breakFound = false;

                while (lineBreaker.MoveNext())
                {
                    if (lineBreaker.Current.Required &&
                        currentLength + lineBreaker.Current.PositionMeasure <= measuredLength)
                    {
                        //Explicit break found
                        breakFound = true;

                        currentPosition = currentLength + lineBreaker.Current.PositionWrap;

                        break;
                    }

                    if (currentLength + lineBreaker.Current.PositionMeasure > measuredLength)
                    {
                        if (paragraphProperties.TextWrapping == TextWrapping.WrapWithOverflow)
                        {
                            if (lastWrapPosition > 0)
                            {
                                currentPosition = lastWrapPosition;

                                breakFound = true;

                                break;
                            }

                            //Find next possible wrap position (overflow)
                            if (index < textRuns.Count - 1)
                            {
                                if (lineBreaker.Current.PositionWrap != currentRun.Text.Length)
                                {
                                    //We already found the next possible wrap position.
                                    breakFound = true;

                                    currentPosition = currentLength + lineBreaker.Current.PositionWrap;

                                    break;
                                }

                                while (lineBreaker.MoveNext() && index < textRuns.Count)
                                {
                                    currentPosition += lineBreaker.Current.PositionWrap;

                                    if (lineBreaker.Current.PositionWrap != currentRun.Text.Length)
                                    {
                                        break;
                                    }

                                    index++;

                                    if (index >= textRuns.Count)
                                    {
                                        break;
                                    }

                                    currentRun = textRuns[index];

                                    lineBreaker = new LineBreakEnumerator(currentRun.Text);
                                }
                            }
                            else
                            {
                                currentPosition = currentLength + lineBreaker.Current.PositionWrap;
                            }

                            breakFound = true;

                            break;
                        }

                        //We overflowed so we use the last available wrap position.
                        currentPosition = lastWrapPosition == 0 ? measuredLength : lastWrapPosition;

                        breakFound = true;

                        break;
                    }

                    if (lineBreaker.Current.PositionMeasure != lineBreaker.Current.PositionWrap)
                    {
                        lastWrapPosition = currentLength + lineBreaker.Current.PositionWrap;
                    }
                }

                if (!breakFound)
                {
                    currentLength += currentRun.Text.Length;

                    continue;
                }

                measuredLength = currentPosition;

                break;
            }

            var splitResult = SplitShapedRuns(textRuns, measuredLength);

            textRange = new TextRange(textRange.Start, measuredLength);

            var remainingCharacters = splitResult.Second;

            var lineBreak = remainingCharacters?.Count > 0 ?
                new TextLineBreak(currentLineBreak?.TextEndOfLine, flowDirection, remainingCharacters) :
                null;

            if (lineBreak is null && currentLineBreak?.TextEndOfLine != null)
            {
                lineBreak = new TextLineBreak(currentLineBreak.TextEndOfLine, flowDirection);
            }

            TextLineImpl.SortRuns(splitResult.First);

            var textLine = new TextLineImpl(splitResult.First, textRange, paragraphWidth, paragraphProperties, flowDirection,
                lineBreak);

            return textLine.FinalizeLine();
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
            public TextRun? Current { get; private set; }

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

                return true;
            }
        }

        /// <summary>
        /// Creates a shaped symbol.
        /// </summary>
        /// <param name="textRun">The symbol run to shape.</param>
        /// <param name="flowDirection">The flow direction.</param>
        /// <returns>
        /// The shaped symbol.
        /// </returns>
        internal static ShapedTextCharacters CreateSymbol(TextRun textRun, FlowDirection flowDirection)
        {
            var textShaper = TextShaper.Current;

            var glyphTypeface = textRun.Properties!.Typeface.GlyphTypeface;

            var fontRenderingEmSize = textRun.Properties.FontRenderingEmSize;

            var cultureInfo = textRun.Properties.CultureInfo;

            var shapedBuffer = textShaper.ShapeText(textRun.Text, glyphTypeface, fontRenderingEmSize, cultureInfo, (sbyte)flowDirection);

            return new ShapedTextCharacters(shapedBuffer, textRun.Properties);
        }
    }
}
