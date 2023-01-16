using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting
{
    internal class TextFormatterImpl : TextFormatter
    {
        private static readonly char[] s_empty = { ' ' };
        private static readonly char[] s_defaultText = new char[TextRun.DefaultTextSourceLength];

        [ThreadStatic] private static BidiData? t_bidiData;
        [ThreadStatic] private static BidiAlgorithm? t_bidiAlgorithm;

        /// <inheritdoc cref="TextFormatter.FormatLine"/>
        public override TextLine FormatLine(ITextSource textSource, int firstTextSourceIndex, double paragraphWidth,
            TextParagraphProperties paragraphProperties, TextLineBreak? previousLineBreak = null)
        {
            var textWrapping = paragraphProperties.TextWrapping;
            FlowDirection resolvedFlowDirection;
            TextLineBreak? nextLineBreak = null;
            IReadOnlyList<TextRun> textRuns;

            var fetchedRuns = FetchTextRuns(textSource, firstTextSourceIndex,
                out var textEndOfLine, out var textSourceLength);

            if (previousLineBreak?.RemainingRuns is { } remainingRuns)
            {
                resolvedFlowDirection = previousLineBreak.FlowDirection;
                textRuns = remainingRuns;
                nextLineBreak = previousLineBreak;
            }
            else
            {
                textRuns = ShapeTextRuns(fetchedRuns, paragraphProperties, out resolvedFlowDirection);

                if (nextLineBreak == null && textEndOfLine != null)
                {
                    nextLineBreak = new TextLineBreak(textEndOfLine, resolvedFlowDirection);
                }
            }

            TextLineImpl textLine;

            switch (textWrapping)
            {
                case TextWrapping.NoWrap:
                    {
                        textLine = new TextLineImpl(textRuns.ToArray(), firstTextSourceIndex, textSourceLength,
                            paragraphWidth, paragraphProperties, resolvedFlowDirection, nextLineBreak);

                        textLine.FinalizeLine();

                        break;
                    }
                case TextWrapping.WrapWithOverflow:
                case TextWrapping.Wrap:
                    {
                        textLine = PerformTextWrapping(textRuns, firstTextSourceIndex, paragraphWidth, paragraphProperties,
                            resolvedFlowDirection, nextLineBreak);
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
        internal static SplitResult<IReadOnlyList<TextRun>> SplitTextRuns(IReadOnlyList<TextRun> textRuns, int length)
        {
            var currentLength = 0;

            for (var i = 0; i < textRuns.Count; i++)
            {
                var currentRun = textRuns[i];

                if (currentLength + currentRun.Length < length)
                {
                    currentLength += currentRun.Length;

                    continue;
                }

                var firstCount = currentRun.Length >= 1 ? i + 1 : i;

                var first = new List<TextRun>(firstCount);

                if (firstCount > 1)
                {
                    for (var j = 0; j < i; j++)
                    {
                        first.Add(textRuns[j]);
                    }
                }

                var secondCount = textRuns.Count - firstCount;

                if (currentLength + currentRun.Length == length)
                {
                    var second = secondCount > 0 ? new List<TextRun>(secondCount) : null;

                    if (second != null)
                    {
                        var offset = currentRun.Length >= 1 ? 1 : 0;

                        for (var j = 0; j < secondCount; j++)
                        {
                            second.Add(textRuns[i + j + offset]);
                        }
                    }

                    first.Add(currentRun);

                    return new SplitResult<IReadOnlyList<TextRun>>(first, second);
                }
                else
                {
                    secondCount++;

                    var second = new List<TextRun>(secondCount);

                    if (currentRun is ShapedTextRun shapedTextCharacters)
                    {
                        var split = shapedTextCharacters.Split(length - currentLength);

                        first.Add(split.First);

                        second.Add(split.Second!);
                    }

                    for (var j = 1; j < secondCount; j++)
                    {
                        second.Add(textRuns[i + j]);
                    }

                    return new SplitResult<IReadOnlyList<TextRun>>(first, second);
                }
            }

            return new SplitResult<IReadOnlyList<TextRun>>(textRuns, null);
        }

        /// <summary>
        /// Shape specified text runs with specified paragraph embedding.
        /// </summary>
        /// <param name="textRuns">The text runs to shape.</param>
        /// <param name="paragraphProperties">The default paragraph properties.</param>
        /// <param name="resolvedFlowDirection">The resolved flow direction.</param>
        /// <returns>
        /// A list of shaped text characters.
        /// </returns>
        private static List<TextRun> ShapeTextRuns(List<TextRun> textRuns, TextParagraphProperties paragraphProperties,
            out FlowDirection resolvedFlowDirection)
        {
            var flowDirection = paragraphProperties.FlowDirection;
            var shapedRuns = new List<TextRun>();

            if (textRuns.Count == 0)
            {
                resolvedFlowDirection = flowDirection;
                return shapedRuns;
            }


            var biDiData = t_bidiData ??= new BidiData();
            biDiData.Reset();
            biDiData.ParagraphEmbeddingLevel = (sbyte)flowDirection;

            foreach (var textRun in textRuns)
            {
                ReadOnlySpan<char> text;
                if (!textRun.Text.IsEmpty)
                    text = textRun.Text.Span;
                else if (textRun.Length == TextRun.DefaultTextSourceLength)
                    text = s_defaultText;
                else
                    text = new char[textRun.Length];

                biDiData.Append(text);
            }

            var biDi = t_bidiAlgorithm ??= new BidiAlgorithm();

            biDi.Process(biDiData);

            var resolvedEmbeddingLevel = biDi.ResolveEmbeddingLevel(biDiData.Classes);

            resolvedFlowDirection =
                (resolvedEmbeddingLevel & 1) == 0 ? FlowDirection.LeftToRight : FlowDirection.RightToLeft;

            var processedRuns = new List<TextRun>(textRuns.Count);

            CoalesceLevels(textRuns, biDi.ResolvedLevels, processedRuns);

            for (var index = 0; index < processedRuns.Count; index++)
            {
                var currentRun = processedRuns[index];

                switch (currentRun)
                {
                    case UnshapedTextRun shapeableRun:
                        {
                            var groupedRuns = new List<UnshapedTextRun>(2) { shapeableRun };
                            var text = shapeableRun.Text;

                            while (index + 1 < processedRuns.Count)
                            {
                                if (processedRuns[index + 1] is not UnshapedTextRun nextRun)
                                {
                                    break;
                                }

                                if (shapeableRun.BidiLevel == nextRun.BidiLevel
                                    && TryJoinContiguousMemories(text, nextRun.Text, out var joinedText)
                                    && CanShapeTogether(shapeableRun.Properties, nextRun.Properties))
                                {
                                    groupedRuns.Add(nextRun);
                                    index++;
                                    shapeableRun = nextRun;
                                    text = joinedText;
                                    continue;
                                }

                                break;
                            }

                            var shaperOptions = new TextShaperOptions(currentRun.Properties!.Typeface.GlyphTypeface,
                                        currentRun.Properties.FontRenderingEmSize,
                                         shapeableRun.BidiLevel, currentRun.Properties.CultureInfo,
                                         paragraphProperties.DefaultIncrementalTab, paragraphProperties.LetterSpacing);

                            ShapeTogether(groupedRuns, text, shaperOptions, shapedRuns);

                            break;
                        }
                    default:
                        {
                            shapedRuns.Add(currentRun);

                            break;
                        }
                }
            }

            return shapedRuns;
        }

        /// <summary>
        /// Tries to join two potnetially contiguous memory regions.
        /// </summary>
        /// <param name="x">The first memory region.</param>
        /// <param name="y">The second memory region.</param>
        /// <param name="joinedMemory">On success, a memory region representing the union of the two regions.</param>
        /// <returns>true if the two regions were contigous; false otherwise.</returns>
        private static bool TryJoinContiguousMemories(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y,
            out ReadOnlyMemory<char> joinedMemory)
        {
            if (MemoryMarshal.TryGetString(x, out var xString, out var xStart, out var xLength))
            {
                if (MemoryMarshal.TryGetString(y, out var yString, out var yStart, out var yLength)
                    && ReferenceEquals(xString, yString)
                    && TryGetContiguousStart(xStart, xLength, yStart, yLength, out var joinedStart))
                {
                    joinedMemory = xString.AsMemory(joinedStart, xLength + yLength);
                    return true;
                }
            }

            else if (MemoryMarshal.TryGetArray(x, out var xSegment))
            {
                if (MemoryMarshal.TryGetArray(y, out var ySegment)
                    && ReferenceEquals(xSegment.Array, ySegment.Array)
                    && TryGetContiguousStart(xSegment.Offset, xSegment.Count, ySegment.Offset, ySegment.Count, out var joinedStart))
                {
                    joinedMemory = xSegment.Array.AsMemory(joinedStart, xSegment.Count + ySegment.Count);
                    return true;
                }
            }

            else if (MemoryMarshal.TryGetMemoryManager(x, out MemoryManager<char>? xManager, out xStart, out xLength))
            {
                if (MemoryMarshal.TryGetMemoryManager(y, out MemoryManager<char>? yManager, out var yStart, out var yLength)
                    && ReferenceEquals(xManager, yManager)
                    && TryGetContiguousStart(xStart, xLength, yStart, yLength, out var joinedStart))
                {
                    joinedMemory = xManager.Memory.Slice(joinedStart, xLength + yLength);
                    return true;
                }
            }

            joinedMemory = default;
            return false;

            static bool TryGetContiguousStart(int xStart, int xLength, int yStart, int yLength, out int joinedStart)
            {
                var xRange = (Start: xStart, Length: xLength);
                var yRange = (Start: yStart, Length: yLength);

                var (firstRange, secondRange) = xStart <= yStart ? (xRange, yRange) : (yRange, xRange);
                if (firstRange.Start + firstRange.Length == secondRange.Start)
                {
                    joinedStart = firstRange.Start;
                    return true;
                }

                joinedStart = default;
                return false;
            }
        }


        private static bool CanShapeTogether(TextRunProperties x, TextRunProperties y)
            => MathUtilities.AreClose(x.FontRenderingEmSize, y.FontRenderingEmSize)
               && x.Typeface == y.Typeface
               && x.BaselineAlignment == y.BaselineAlignment;

        private static void ShapeTogether(IReadOnlyList<UnshapedTextRun> textRuns, ReadOnlyMemory<char> text,
            TextShaperOptions options, List<TextRun> results)
        {
            var shapedBuffer = TextShaper.Current.ShapeText(text, options);

            for (var i = 0; i < textRuns.Count; i++)
            {
                var currentRun = textRuns[i];

                var splitResult = shapedBuffer.Split(currentRun.Length);

                results.Add(new ShapedTextRun(splitResult.First, currentRun.Properties));

                shapedBuffer = splitResult.Second!;
            }
        }

        /// <summary>
        /// Coalesces ranges of the same bidi level to form <see cref="UnshapedTextRun"/>
        /// </summary>
        /// <param name="textCharacters">The text characters to form <see cref="UnshapedTextRun"/> from.</param>
        /// <param name="levels">The bidi levels.</param>
        /// <param name="processedRuns">A list that will be filled with the processed runs.</param>
        /// <returns></returns>
        private static void CoalesceLevels(IReadOnlyList<TextRun> textCharacters, ArraySlice<sbyte> levels,
            List<TextRun> processedRuns)
        {
            if (levels.Length == 0)
            {
                return;
            }

            var levelIndex = 0;
            var runLevel = levels[0];

            TextRunProperties? previousProperties = null;
            TextCharacters? currentRun = null;
            ReadOnlyMemory<char> runText = default;

            for (var i = 0; i < textCharacters.Count; i++)
            {
                var j = 0;
                currentRun = textCharacters[i] as TextCharacters;

                if (currentRun == null)
                {
                    var drawableRun = textCharacters[i];

                    processedRuns.Add(drawableRun);

                    levelIndex += drawableRun.Length;

                    continue;
                }

                runText = currentRun.Text;
                var runTextSpan = runText.Span;

                for (; j < runTextSpan.Length;)
                {
                    Codepoint.ReadAt(runTextSpan, j, out var count);

                    if (levelIndex + 1 == levels.Length)
                    {
                        break;
                    }

                    levelIndex++;
                    j += count;

                    if (j == runTextSpan.Length)
                    {
                        currentRun.GetShapeableCharacters(runText.Slice(0, j), runLevel, ref previousProperties,
                            processedRuns);

                        runLevel = levels[levelIndex];

                        continue;
                    }

                    if (levels[levelIndex] == runLevel)
                    {
                        continue;
                    }

                    // End of this run
                    currentRun.GetShapeableCharacters(runText.Slice(0, j), runLevel, ref previousProperties,
                        processedRuns);

                    runText = runText.Slice(j);
                    runTextSpan = runText.Span;

                    j = 0;

                    // Move to next run
                    runLevel = levels[levelIndex];
                }
            }

            if (currentRun is null || runText.IsEmpty)
            {
                return;
            }

            currentRun.GetShapeableCharacters(runText, runLevel, ref previousProperties, processedRuns);
        }

        /// <summary>
        /// Fetches text runs.
        /// </summary>
        /// <param name="textSource">The text source.</param>
        /// <param name="firstTextSourceIndex">The first text source index.</param>
        /// <param name="endOfLine">On return, the end of line, if any.</param>
        /// <param name="textSourceLength">On return, the processed text source length.</param>
        /// <returns>
        /// The formatted text runs.
        /// </returns>
        private static List<TextRun> FetchTextRuns(ITextSource textSource, int firstTextSourceIndex,
            out TextEndOfLine? endOfLine, out int textSourceLength)
        {
            textSourceLength = 0;

            endOfLine = null;

            var textRuns = new List<TextRun>();

            var textRunEnumerator = new TextRunEnumerator(textSource, firstTextSourceIndex);

            while (textRunEnumerator.MoveNext())
            {
                var textRun = textRunEnumerator.Current;

                if (textRun == null)
                {
                    textRuns.Add(new TextEndOfParagraph());

                    textSourceLength += TextRun.DefaultTextSourceLength;

                    break;
                }

                if (textRun is TextEndOfLine textEndOfLine)
                {
                    endOfLine = textEndOfLine;

                    textSourceLength += textEndOfLine.Length;

                    textRuns.Add(textRun);

                    break;
                }

                switch (textRun)
                {
                    case TextCharacters textCharacters:
                        {
                            if (TryGetLineBreak(textCharacters, out var runLineBreak))
                            {
                                var splitResult = new TextCharacters(textCharacters.Text.Slice(0, runLineBreak.PositionWrap),
                                    textCharacters.Properties);

                                textRuns.Add(splitResult);

                                textSourceLength += runLineBreak.PositionWrap;

                                return textRuns;
                            }

                            textRuns.Add(textCharacters);

                            break;
                        }
                    default:
                        {
                            textRuns.Add(textRun);
                            break;
                        }
                }

                textSourceLength += textRun.Length;
            }

            return textRuns;
        }

        private static bool TryGetLineBreak(TextRun textRun, out LineBreak lineBreak)
        {
            lineBreak = default;

            var text = textRun.Text;

            if (text.IsEmpty)
            {
                return false;
            }

            var lineBreakEnumerator = new LineBreakEnumerator(text.Span);

            while (lineBreakEnumerator.MoveNext())
            {
                if (!lineBreakEnumerator.Current.Required)
                {
                    continue;
                }

                lineBreak = lineBreakEnumerator.Current;

                return lineBreak.PositionWrap >= textRun.Length || true;
            }

            return false;
        }

        private static bool TryMeasureLength(IReadOnlyList<TextRun> textRuns, double paragraphWidth, out int measuredLength)
        {
            measuredLength = 0;
            var currentWidth = 0.0;

            foreach (var currentRun in textRuns)
            {
                switch (currentRun)
                {
                    case ShapedTextRun shapedTextCharacters:
                        {
                            if (shapedTextCharacters.ShapedBuffer.Length > 0)
                            {
                                var firstCluster = shapedTextCharacters.ShapedBuffer.GlyphInfos[0].GlyphCluster;
                                var lastCluster = firstCluster;

                                for (var i = 0; i < shapedTextCharacters.ShapedBuffer.Length; i++)
                                {
                                    var glyphInfo = shapedTextCharacters.ShapedBuffer[i];

                                    if (currentWidth + glyphInfo.GlyphAdvance > paragraphWidth)
                                    {
                                        measuredLength += Math.Max(0, lastCluster - firstCluster);

                                        goto found;
                                    }

                                    lastCluster = glyphInfo.GlyphCluster;
                                    currentWidth += glyphInfo.GlyphAdvance;
                                }

                                measuredLength += currentRun.Length;
                            }

                            break;
                        }

                    case DrawableTextRun drawableTextRun:
                        {
                            if (currentWidth + drawableTextRun.Size.Width >= paragraphWidth)
                            {
                                goto found;
                            }

                            measuredLength += currentRun.Length;
                            currentWidth += drawableTextRun.Size.Width;

                            break;
                        }
                    default:
                        {
                            measuredLength += currentRun.Length;

                            break;
                        }
                }
            }

        found:

            return measuredLength != 0;
        }

        /// <summary>
        /// Creates an empty text line.
        /// </summary>
        /// <returns>The empty text line.</returns>
        public static TextLineImpl CreateEmptyTextLine(int firstTextSourceIndex, double paragraphWidth, TextParagraphProperties paragraphProperties)
        {
            var flowDirection = paragraphProperties.FlowDirection;
            var properties = paragraphProperties.DefaultTextRunProperties;
            var glyphTypeface = properties.Typeface.GlyphTypeface;
            var glyph = glyphTypeface.GetGlyph(s_empty[0]);
            var glyphInfos = new[] { new GlyphInfo(glyph, firstTextSourceIndex) };

            var shapedBuffer = new ShapedBuffer(s_empty.AsMemory(), glyphInfos, glyphTypeface, properties.FontRenderingEmSize,
                (sbyte)flowDirection);

            var textRuns = new TextRun[] { new ShapedTextRun(shapedBuffer, properties) };

            return new TextLineImpl(textRuns, firstTextSourceIndex, 0, paragraphWidth, paragraphProperties, flowDirection).FinalizeLine();
        }

        /// <summary>
        /// Performs text wrapping returns a list of text lines.
        /// </summary>
        /// <param name="textRuns"></param>
        /// <param name="firstTextSourceIndex">The first text source index.</param>
        /// <param name="paragraphWidth">The paragraph width.</param>
        /// <param name="paragraphProperties">The text paragraph properties.</param>
        /// <param name="resolvedFlowDirection"></param>
        /// <param name="currentLineBreak">The current line break if the line was explicitly broken.</param>
        /// <returns>The wrapped text line.</returns>
        private static TextLineImpl PerformTextWrapping(IReadOnlyList<TextRun> textRuns, int firstTextSourceIndex,
            double paragraphWidth, TextParagraphProperties paragraphProperties, FlowDirection resolvedFlowDirection,
            TextLineBreak? currentLineBreak)
        {
            if (textRuns.Count == 0)
            {
                return CreateEmptyTextLine(firstTextSourceIndex, paragraphWidth, paragraphProperties);
            }

            if (!TryMeasureLength(textRuns, paragraphWidth, out var measuredLength))
            {
                measuredLength = 1;
            }

            var currentLength = 0;

            var lastWrapPosition = 0;

            var currentPosition = 0;

            for (var index = 0; index < textRuns.Count; index++)
            {
                var breakFound = false;

                var currentRun = textRuns[index];

                switch (currentRun)
                {
                    case ShapedTextRun:
                    {
                            var lineBreaker = new LineBreakEnumerator(currentRun.Text.Span);

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
                                            if (lineBreaker.Current.PositionWrap != currentRun.Length)
                                            {
                                                //We already found the next possible wrap position.
                                                breakFound = true;

                                                currentPosition = currentLength + lineBreaker.Current.PositionWrap;

                                                break;
                                            }

                                            while (lineBreaker.MoveNext() && index < textRuns.Count)
                                            {
                                                currentPosition += lineBreaker.Current.PositionWrap;

                                                if (lineBreaker.Current.PositionWrap != currentRun.Length)
                                                {
                                                    break;
                                                }

                                                index++;

                                                if (index >= textRuns.Count)
                                                {
                                                    break;
                                                }

                                                currentRun = textRuns[index];

                                                lineBreaker = new LineBreakEnumerator(currentRun.Text.Span);
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

                            break;
                        }
                }

                if (!breakFound)
                {
                    currentLength += currentRun.Length;

                    continue;
                }

                measuredLength = currentPosition;

                break;
            }

            var (preSplitRuns, postSplitRuns) = SplitTextRuns(textRuns, measuredLength);

            var lineBreak = postSplitRuns?.Count > 0 ?
                new TextLineBreak(null, resolvedFlowDirection, postSplitRuns) :
                null;

            if (lineBreak is null && currentLineBreak?.TextEndOfLine != null)
            {
                lineBreak = new TextLineBreak(currentLineBreak.TextEndOfLine, resolvedFlowDirection);
            }

            var textLine = new TextLineImpl(preSplitRuns.ToArray(), firstTextSourceIndex, measuredLength,
                paragraphWidth, paragraphProperties, resolvedFlowDirection,
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

                if (Current.Length == 0)
                {
                    return false;
                }

                _pos += Current.Length;

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
        internal static ShapedTextRun CreateSymbol(TextRun textRun, FlowDirection flowDirection)
        {
            var textShaper = TextShaper.Current;

            var glyphTypeface = textRun.Properties!.Typeface.GlyphTypeface;

            var fontRenderingEmSize = textRun.Properties.FontRenderingEmSize;

            var cultureInfo = textRun.Properties.CultureInfo;

            var shaperOptions = new TextShaperOptions(glyphTypeface, fontRenderingEmSize, (sbyte)flowDirection, cultureInfo);

            var shapedBuffer = textShaper.ShapeText(textRun.Text, shaperOptions);

            return new ShapedTextRun(shapedBuffer, textRun.Properties);
        }
    }
}
