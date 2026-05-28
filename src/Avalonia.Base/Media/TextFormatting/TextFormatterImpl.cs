// ReSharper disable ForCanBeConvertedToForeach
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Utilities;
using static Avalonia.Media.TextFormatting.FormattingObjectPool;

namespace Avalonia.Media.TextFormatting
{
    internal sealed class TextFormatterImpl : TextFormatter
    {
        private static readonly char[] s_empty = { ' ' };
        private static readonly string s_defaultText = new string('a', TextRun.DefaultTextSourceLength);

        [ThreadStatic] private static BidiData? t_bidiData;
        [ThreadStatic] private static BidiAlgorithm? t_bidiAlgorithm;

        /// <inheritdoc/>
        public override TextLine? FormatLine(ITextSource textSource, int firstTextSourceIndex, double paragraphWidth,
            TextParagraphProperties paragraphProperties, TextLineBreak? previousLineBreak = null)
        {
            return FormatLine(textSource, firstTextSourceIndex, paragraphWidth,
                paragraphProperties, previousLineBreak, null);
        }

        /// <inheritdoc/>
        public override TextLine? FormatLine(ITextSource textSource, int firstTextSourceIndex, double paragraphWidth,
            TextParagraphProperties paragraphProperties, TextLineBreak? previousLineBreak,
            TextRunCache? textRunCache)
        {
            TextLineBreak? nextLineBreak = null;
            var objectPool = FormattingObjectPool.Instance;
            var fontManager = FontManager.Current;

            // we've wrapped the previous line and need to continue wrapping: ignore the textSource and do that instead
            if (previousLineBreak is WrappingTextLineBreak wrappingTextLineBreak
                && wrappingTextLineBreak.AcquireRemainingRuns() is { } remainingRuns
                && paragraphProperties.TextWrapping != TextWrapping.NoWrap)
            {
                return PerformTextWrapping(remainingRuns, true, firstTextSourceIndex, paragraphWidth,
                    paragraphProperties, previousLineBreak.FlowDirection, previousLineBreak, objectPool);
            }

            // Try to use cached shaped runs to avoid redundant shaping/bidi processing.
            if (textRunCache != null
                && textRunCache.TryGetShapedRuns(firstTextSourceIndex, out var cached))
            {
                return FormatLineFromCache(cached, firstTextSourceIndex, paragraphWidth,
                    paragraphProperties, objectPool);
            }

            RentedList<TextRun>? fetchedRuns = null;
            RentedList<TextRun>? shapedTextRuns = null;
            try
            {
                fetchedRuns = FetchTextRuns(textSource, firstTextSourceIndex, objectPool, out var textEndOfLine,
                    out var textSourceLength);

                if (fetchedRuns.Count == 0)
                {
                    return null;
                }

                shapedTextRuns = ShapeTextRuns(fetchedRuns, paragraphProperties, objectPool, fontManager,
                    out var resolvedFlowDirection);

                if (nextLineBreak == null && textEndOfLine != null)
                {
                    nextLineBreak = new TextLineBreak(textEndOfLine, resolvedFlowDirection);
                }

                // Store shaped runs in cache for reuse. The cache takes its own references;
                // the formatter keeps the fresh-from-shape references for the current line.
                if (textRunCache != null)
                {
                    textRunCache.Add(firstTextSourceIndex,
                        new CachedShapingResult(shapedTextRuns.ToArray(), resolvedFlowDirection,
                            textEndOfLine, textSourceLength));
                }

                switch (paragraphProperties.TextWrapping)
                {
                    case TextWrapping.NoWrap:
                        {
                            var lineRuns = shapedTextRuns.ToArray();

                            var textLine = new TextLineImpl(lineRuns, firstTextSourceIndex,
                                textSourceLength,
                                paragraphWidth, paragraphProperties, resolvedFlowDirection, nextLineBreak);

                            textLine.FinalizeLine();

                            return textLine;
                        }
                    case TextWrapping.WrapWithOverflow:
                    case TextWrapping.Wrap:
                        {
                            return PerformTextWrapping(shapedTextRuns, false, firstTextSourceIndex, paragraphWidth,
                                paragraphProperties, resolvedFlowDirection, nextLineBreak, objectPool);
                        }
                    default:
                        throw new ArgumentOutOfRangeException(nameof(paragraphProperties.TextWrapping));
                }
            }
            finally
            {
                objectPool.TextRunLists.Return(ref shapedTextRuns);
                objectPool.TextRunLists.Return(ref fetchedRuns);
            }
        }

        /// <summary>
        /// Formats a line from cached shaped runs, skipping shaping and bidi processing.
        /// </summary>
        private static TextLine FormatLineFromCache(CachedShapingResult cached, int firstTextSourceIndex,
            double paragraphWidth, TextParagraphProperties paragraphProperties, FormattingObjectPool objectPool)
        {
            var resolvedFlowDirection = cached.ResolvedFlowDirection;

            TextLineBreak? nextLineBreak = null;

            if (cached.TextEndOfLine != null)
            {
                nextLineBreak = new TextLineBreak(cached.TextEndOfLine, resolvedFlowDirection);
            }

            switch (paragraphProperties.TextWrapping)
            {
                case TextWrapping.NoWrap:
                    {
                        var lineRuns = AddRefShapedRuns(cached.ShapedRuns);

                        var textLine = new TextLineImpl(lineRuns, firstTextSourceIndex,
                            cached.TextSourceLength,
                            paragraphWidth, paragraphProperties, resolvedFlowDirection, nextLineBreak);

                        textLine.FinalizeLine();

                        return textLine;
                    }
                case TextWrapping.WrapWithOverflow:
                case TextWrapping.Wrap:
                    {
                        var runs = new List<TextRun>(cached.ShapedRuns.Length);

                        for (var i = 0; i < cached.ShapedRuns.Length; i++)
                        {
                            runs.Add(cached.ShapedRuns[i] is ShapedTextRun shaped
                                ? shaped.AddRef()
                                : cached.ShapedRuns[i]);
                        }

                        return PerformTextWrapping(runs, false, firstTextSourceIndex,
                            paragraphWidth, paragraphProperties, resolvedFlowDirection,
                            nextLineBreak, objectPool);
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(paragraphProperties.TextWrapping));
            }
        }

        /// <summary>
        /// Produces an array of text runs for a line, adding an extra reference to each
        /// <see cref="ShapedTextRun"/> so that the caller owns a disposable reference.
        /// </summary>
        private static TextRun[] AddRefShapedRuns(IReadOnlyList<TextRun> runs)
        {
            var result = new TextRun[runs.Count];

            for (var i = 0; i < runs.Count; i++)
            {
                result[i] = runs[i] is ShapedTextRun shaped ? shaped.AddRef() : runs[i];
            }

            return result;
        }

        /// <summary>
        /// Split a sequence of runs into two segments at specified length.
        /// </summary>
        /// <param name="textRuns">The text run's.</param>
        /// <param name="length">The length to split at.</param>
        /// <param name="objectPool">A pool used to get reusable formatting objects.</param>
        /// <returns>The split text runs.</returns>
        internal static SplitResult<RentedList<TextRun>> SplitTextRuns(IReadOnlyList<TextRun> textRuns, int length,
            FormattingObjectPool objectPool)
            => SplitTextRuns(textRuns, length, objectPool, out _);

        /// <summary>
        /// Split a sequence of runs into two segments at specified length. The actual
        /// length of the first segment (which may differ from <paramref name="length"/>
        /// when the split lands on a cluster boundary) is returned via
        /// <paramref name="firstLength"/>. This lets the wrap caller avoid a separate
        /// second pass to sum run lengths.
        /// </summary>
        internal static SplitResult<RentedList<TextRun>> SplitTextRuns(IReadOnlyList<TextRun> textRuns, int length,
            FormattingObjectPool objectPool, out int firstLength)
        {
            if(length == 0)
            {
                var second = objectPool.TextRunLists.Rent();

                for (var i = 0; i < textRuns.Count; i++)
                {
                    second.Add(textRuns[i]);
                }

                firstLength = 0;
                return new SplitResult<RentedList<TextRun>>(null, second);
            }

            var first = objectPool.TextRunLists.Rent();
            var currentLength = 0;

            for (var i = 0; i < textRuns.Count; i++)
            {
                var currentRun = textRuns[i];
                var currentRunLength = currentRun.Length;

                if (currentLength + currentRunLength < length)
                {
                    currentLength += currentRunLength;

                    continue;
                }

                var firstCount = currentRunLength >= 1 ? i + 1 : i;

                if (firstCount > 1)
                {
                    for (var j = 0; j < i; j++)
                    {
                        first.Add(textRuns[j]);
                    }
                }

                var secondCount = textRuns.Count - firstCount;

                if (currentLength + currentRunLength == length)
                {
                    var second = secondCount > 0 ? objectPool.TextRunLists.Rent() : null;

                    if (second != null)
                    {
                        var offset = currentRunLength >= 1 ? 1 : 0;

                        for (var j = 0; j < secondCount; j++)
                        {
                            second.Add(textRuns[i + j + offset]);
                        }
                    }

                    first.Add(currentRun);

                    firstLength = currentLength + currentRunLength;
                    return new SplitResult<RentedList<TextRun>>(first, second);
                }
                else
                {
                    secondCount++;

                    var second = objectPool.TextRunLists.Rent();
                    var addedFirstLength = 0;
                    int trailingLoopStart;

                    if (currentRun is ShapedTextRun shapedTextCharacters)
                    {
                        var split = shapedTextCharacters.Split(length - currentLength);

                        if(split.First is not null)
                        {
                            first.Add(split.First);
                            addedFirstLength = split.First.Length;
                        }

                        if (split.Second != null)
                        {
                            second.Add(split.Second);
                        }

                        // The split produced fresh ShapedTextRuns for each half, so the
                        // caller's reference to the original is no longer needed — release it.
                        shapedTextCharacters.Dispose();

                        // currentRun is consumed by the split; the trailing loop adds the
                        // runs *after* it.
                        trailingLoopStart = 1;
                    }
                    else if (currentLength == 0)
                    {
                        // Non-splittable run at the very start of the list, asked to split
                        // strictly inside it. Snapping before would leave first empty and
                        // the wrap caller would loop forever — same situation as the wrap
                        // algorithm's "include at least one cluster" overflow rule. Place
                        // currentRun in first and let the line overflow.
                        first.Add(currentRun);
                        addedFirstLength = currentRunLength;
                        trailingLoopStart = 1;
                    }
                    else
                    {
                        // Non-splittable run at the split point. Snap the boundary BEFORE
                        // it: currentRun goes into second along with the remaining runs,
                        // first ends at currentLength (shorter than requested but
                        // content-preserving). Without this branch the run would be
                        // dropped from both halves.
                        trailingLoopStart = 0;
                    }

                    for (var j = trailingLoopStart; j < secondCount; j++)
                    {
                        second.Add(textRuns[i + j]);
                    }

                    firstLength = currentLength + addedFirstLength;
                    return new SplitResult<RentedList<TextRun>>(first, second);
                }
            }

            for (var i = 0; i < textRuns.Count; i++)
            {
                first.Add(textRuns[i]);
            }

            firstLength = currentLength;
            return new SplitResult<RentedList<TextRun>>(first, null);
        }

        /// <summary>
        /// Shape specified text runs with specified paragraph embedding.
        /// </summary>
        /// <param name="textRuns">The text runs to shape.</param>
        /// <param name="paragraphProperties">The default paragraph properties.</param>
        /// <param name="resolvedFlowDirection">The resolved flow direction.</param>
        /// <param name="objectPool">A pool used to get reusable formatting objects.</param>
        /// <param name="fontManager">The font manager to use.</param>
        /// <returns>
        /// A list of shaped text characters.
        /// </returns>
        private static RentedList<TextRun> ShapeTextRuns(IReadOnlyList<TextRun> textRuns,
            TextParagraphProperties paragraphProperties, FormattingObjectPool objectPool,
            FontManager fontManager, out FlowDirection resolvedFlowDirection)
        {
            var flowDirection = paragraphProperties.FlowDirection;
            var shapedRuns = objectPool.TextRunLists.Rent();

            if (textRuns.Count == 0)
            {
                resolvedFlowDirection = flowDirection;
                return shapedRuns;
            }

            var bidiData = t_bidiData ??= new();
            bidiData.Reset();
            bidiData.ParagraphEmbeddingLevel = (sbyte)flowDirection;

            for (var i = 0; i < textRuns.Count; ++i)
            {
                var textRun = textRuns[i];

                ReadOnlySpan<char> text;
                if (!textRun.Text.IsEmpty)
                    text = textRun.Text.Span;
                else if (textRun.Length == TextRun.DefaultTextSourceLength)
                    text = s_defaultText.AsSpan();
                else
                {
                    text = new string('a', textRun.Length).AsSpan();
                }

                bidiData.Append(text);
            }

            var bidiAlgorithm = t_bidiAlgorithm ??= new();
            bidiAlgorithm.Process(bidiData);

            var resolvedEmbeddingLevel = bidiAlgorithm.ResolveEmbeddingLevel(bidiData.Classes);

            resolvedFlowDirection =
                (resolvedEmbeddingLevel & 1) == 0 ? FlowDirection.LeftToRight : FlowDirection.RightToLeft;

            var processedRuns = objectPool.TextRunLists.Rent();
            var groupedRuns = objectPool.UnshapedTextRunLists.Rent();

            try
            {
                CoalesceLevels(textRuns, bidiAlgorithm.ResolvedLevels.Span, fontManager, processedRuns);

                bidiData.Reset();
                bidiAlgorithm.Reset();


                var textShaper = TextShaper.Current;

                for (var index = 0; index < processedRuns.Count; index++)
                {
                    var currentRun = processedRuns[index];

                    switch (currentRun)
                    {
                        case UnshapedTextRun shapeableRun:
                            {
                                groupedRuns.Clear();
                                groupedRuns.Add(shapeableRun);

                                var text = shapeableRun.Text;
                                var properties = shapeableRun.Properties;

                                while (index + 1 < processedRuns.Count)
                                {
                                    if (processedRuns[index + 1] is not UnshapedTextRun nextRun)
                                    {
                                        break;
                                    }

                                    if (shapeableRun.BidiLevel == nextRun.BidiLevel
                                        && TryJoinContiguousMemories(text, nextRun.Text, out var joinedText)
                                        && CanShapeTogether(properties, nextRun.Properties))
                                    {
                                        groupedRuns.Add(nextRun);
                                        index++;
                                        shapeableRun = nextRun;
                                        text = joinedText;
                                        continue;
                                    }

                                    break;
                                }

                                var shaperOptions = new TextShaperOptions(
                                    properties.CachedGlyphTypeface,
                                    properties.FontRenderingEmSize,
                                    shapeableRun.BidiLevel,
                                    properties.CultureInfo,
                                    paragraphProperties.DefaultIncrementalTab,
                                    paragraphProperties.LetterSpacing,
                                    properties.FontFeatures);

                                ShapeTogether(groupedRuns, text, shaperOptions, textShaper, shapedRuns);

                                break;
                            }
                        default:
                            {
                                shapedRuns.Add(currentRun);

                                break;
                            }
                    }
                }
            }
            finally
            {
                objectPool.TextRunLists.Return(ref processedRuns);
                objectPool.UnshapedTextRunLists.Return(ref groupedRuns);
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
            TextShaperOptions options, TextShaper textShaper, RentedList<TextRun> results)
        {
            var shapedBuffer = textShaper.ShapeText(text, options);

            var previousLength = 0;

            for (var i = 0; i < textRuns.Count; i++)
            {
                var currentRun = textRuns[i];

                var splitResult = shapedBuffer.Split(previousLength + currentRun.Length);

                if (splitResult.First is null || splitResult.First.Length == 0)
                {
                    previousLength += currentRun.Length;
                }
                else
                {
                    previousLength = 0;

                    results.Add(new ShapedTextRun(splitResult.First, currentRun.Properties));
                }
              
                if(splitResult.Second is null)
                {
                    return;
                }

                shapedBuffer = splitResult.Second;
            }
        }

        /// <summary>
        /// Coalesces ranges of the same bidi level to form <see cref="UnshapedTextRun"/>
        /// </summary>
        /// <param name="textCharacters">The text characters to form <see cref="UnshapedTextRun"/> from.</param>
        /// <param name="levels">The bidi levels.</param>
        /// <param name="fontManager">The font manager to use.</param>
        /// <param name="processedRuns">A list that will be filled with the processed runs.</param>
        /// <returns></returns>
        private static void CoalesceLevels(IReadOnlyList<TextRun> textCharacters, ReadOnlySpan<sbyte> levels,
            FontManager fontManager, RentedList<TextRun> processedRuns)
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
                        currentRun.GetShapeableCharacters(runText.Slice(0, j), runLevel, fontManager,
                            ref previousProperties, processedRuns);

                        runLevel = levels[levelIndex];

                        continue;
                    }

                    if (levels[levelIndex] == runLevel)
                    {
                        continue;
                    }

                    // End of this run
                    currentRun.GetShapeableCharacters(runText.Slice(0, j), runLevel, fontManager,
                        ref previousProperties, processedRuns);

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

            currentRun.GetShapeableCharacters(runText, runLevel, fontManager, ref previousProperties, processedRuns);
        }

        /// <summary>
        /// Fetches text runs.
        /// </summary>
        /// <param name="textSource">The text source.</param>
        /// <param name="firstTextSourceIndex">The first text source index.</param>
        /// <param name="objectPool">A pool used to get reusable formatting objects.</param>
        /// <param name="endOfLine">On return, the end of line, if any.</param>
        /// <param name="textSourceLength">On return, the processed text source length.</param>
        /// <returns>
        /// The formatted text runs.
        /// </returns>
        private static RentedList<TextRun> FetchTextRuns(ITextSource textSource, int firstTextSourceIndex,
            FormattingObjectPool objectPool, out TextEndOfLine? endOfLine, out int textSourceLength)
        {
            textSourceLength = 0;

            endOfLine = null;

            var textRuns = objectPool.TextRunLists.Rent();

            var textRunEnumerator = new TextRunEnumerator(textSource, firstTextSourceIndex);

            while (textRunEnumerator.MoveNext())
            {
                TextRun textRun = textRunEnumerator.Current!;

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

            while (lineBreakEnumerator.MoveNext(out lineBreak))
            {
                if (!lineBreak.Required)
                {
                    continue;
                }

                return lineBreak.PositionWrap >= textRun.Length || true;
            }

            return false;
        }

        private static int MeasureLength(IReadOnlyList<TextRun> textRuns, double paragraphWidth)
        {
            var measuredLength = 0;
            var currentWidth = 0.0;
            var runIndex = 0;

            for (; runIndex < textRuns.Count; ++runIndex)
            {
                var currentRun = textRuns[runIndex];

                switch (currentRun)
                {
                    case ShapedTextRun shapedTextCharacters:
                        {
                            // cluster-width prefix sum lets us answer "how much fits"
                            // in O(log clusters) instead of walking every glyph. The total
                            // advance and the per-cluster start char are cached on the
                            // ShapedBuffer (which lives in the run cache), so the first
                            // layout pays the O(glyphs) cost and every subsequent layout
                            // is constant-time.
                            var buffer = shapedTextCharacters.ShapedBuffer;

                            if (buffer.Length == 0)
                            {
                                break;
                            }

                            var remaining = paragraphWidth - currentWidth;
                            var bufferWidth = buffer.TotalGlyphAdvance;

                            if (!MathUtilities.GreaterThan(bufferWidth, remaining))
                            {
                                // Whole buffer fits consume it and continue to the next run.
                                currentWidth += bufferWidth;
                                measuredLength += currentRun.Length;
                                break;
                            }

                            // Some part of the buffer overflows: find the cluster boundary.
                            var runLength = buffer.MeasureCharactersThatFit(remaining, out _);

                            // "Include at least one cluster" rule preserves the existing
                            // contract that the caller always advances by at least one
                            // grapheme even when the first cluster overflows the line.
                            if (runLength == 0 && measuredLength == 0)
                            {
                                runLength = buffer.FirstClusterCharLength;
                            }

                            measuredLength += runLength;

                            if (runIndex < textRuns.Count - 1 && runLength == currentRun.Length &&
                                textRuns[runIndex + 1] is TextEndOfLine endOfLine)
                            {
                                measuredLength += endOfLine.Length;
                            }

                            return measuredLength;
                        }

                    case DrawableTextRun drawableTextRun:
                        {
                            if (MathUtilities.GreaterThan(currentWidth + drawableTextRun.Size.Width, paragraphWidth))
                            {
                                return measuredLength;
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

            return measuredLength;
        }

        /// <summary>
        /// Creates an empty text line.
        /// </summary>
        /// <returns>The empty text line.</returns>
        public static TextLineImpl CreateEmptyTextLine(int firstTextSourceIndex, double paragraphWidth,
            TextParagraphProperties paragraphProperties)
        {
            var flowDirection = paragraphProperties.FlowDirection;
            var properties = paragraphProperties.DefaultTextRunProperties;
            var glyphTypeface = properties.CachedGlyphTypeface;
            var glyph = glyphTypeface.CharacterToGlyphMap[s_empty[0]];
            var glyphInfos = new[] { new GlyphInfo(glyph, firstTextSourceIndex, 0.0) };

            var shapedBuffer = new ShapedBuffer(s_empty.AsMemory(), glyphInfos, glyphTypeface, properties.FontRenderingEmSize,
                (sbyte)flowDirection);

            var textRuns = new TextRun[] { new ShapedTextRun(shapedBuffer, properties) };

            var line = new TextLineImpl(textRuns, firstTextSourceIndex, 0, paragraphWidth, paragraphProperties, flowDirection);

            line.FinalizeLine();

            return line;
        }

        /// <summary>
        /// Performs text wrapping returns a list of text lines.
        /// </summary>
        /// <param name="textRuns"></param>
        /// <param name="canReuseTextRunList">Whether <see cref="TextRun"/> can be reused to store the split runs.</param>
        /// <param name="firstTextSourceIndex">The first text source index.</param>
        /// <param name="paragraphWidth">The paragraph width.</param>
        /// <param name="paragraphProperties">The text paragraph properties.</param>
        /// <param name="resolvedFlowDirection"></param>
        /// <param name="currentLineBreak">The current line break if the line was explicitly broken.</param>
        /// <param name="objectPool">A pool used to get reusable formatting objects.</param>
        /// <returns>The wrapped text line.</returns>
        private static TextLineImpl PerformTextWrapping(List<TextRun> textRuns, bool canReuseTextRunList,
            int firstTextSourceIndex, double paragraphWidth, TextParagraphProperties paragraphProperties,
            FlowDirection resolvedFlowDirection, TextLineBreak? currentLineBreak, FormattingObjectPool objectPool)
        {
            if (textRuns.Count == 0)
            {
                return CreateEmptyTextLine(firstTextSourceIndex, paragraphWidth, paragraphProperties);
            }

            var measuredLength = MeasureLength(textRuns, paragraphWidth);

            if(measuredLength == 0)
            {
                if(paragraphProperties.TextWrapping == TextWrapping.NoWrap)
                {
                    for (int i = 0; i < textRuns.Count; i++)
                    {
                        measuredLength += textRuns[i].Length;
                    }
                }
                else
                {
                    var firstRun = textRuns[0];

                    if(firstRun is ShapedTextRun)
                    {
                        var graphemeEnumerator = new GraphemeEnumerator(firstRun.Text.Span);

                        if(graphemeEnumerator.MoveNext(out var grapheme))
                        {
                            measuredLength = grapheme.Length;
                        }
                        else
                        {
                            measuredLength = 1;
                        }
                    }
                    else
                    {
                        measuredLength = firstRun.Length;
                    }
                }
            }

            var currentLength = 0;

            var lastWrapPosition = 0;

            var currentPosition = 0;

            var wrappingMode = paragraphProperties.TextWrapping;
            var runCount = textRuns.Count;

            for (var index = 0; index < runCount; index++)
            {
                var breakFound = false;

                var currentRun = textRuns[index];
                var currentRunLength = currentRun.Length;

                switch (currentRun)
                {
                    case ShapedTextRun:
                        {
                            var lineBreaker = new LineBreakEnumerator(currentRun.Text.Span);

                            while (lineBreaker.MoveNext(out var lineBreak))
                            {
                                if (lineBreak.Required &&
                                    currentLength + lineBreak.PositionMeasure <= measuredLength)
                                {
                                    //Explicit break found
                                    breakFound = true;

                                    currentPosition = currentLength + lineBreak.PositionWrap;

                                    break;
                                }

                                if (currentLength + lineBreak.PositionMeasure > measuredLength)
                                {
                                    if (wrappingMode == TextWrapping.WrapWithOverflow)
                                    {
                                        if (lastWrapPosition > 0)
                                        {
                                            currentPosition = lastWrapPosition;

                                            breakFound = true;

                                            break;
                                        }

                                        //Find next possible wrap position (overflow)
                                        if (index < runCount - 1)
                                        {
                                            if (lineBreak.PositionWrap != currentRunLength)
                                            {
                                                //We already found the next possible wrap position.
                                                breakFound = true;

                                                currentPosition = currentLength + lineBreak.PositionWrap;

                                                break;
                                            }

                                            while (lineBreaker.MoveNext(out lineBreak))
                                            {
                                                currentPosition += lineBreak.PositionWrap;

                                                if (lineBreak.PositionWrap != currentRunLength)
                                                {
                                                    break;
                                                }

                                                index++;

                                                if (index >= runCount)
                                                {
                                                    break;
                                                }

                                                currentRun = textRuns[index];
                                                currentRunLength = currentRun.Length;

                                                lineBreaker = new LineBreakEnumerator(currentRun.Text.Span);
                                            }
                                        }
                                        else
                                        {
                                            currentPosition = currentLength + lineBreak.PositionWrap;
                                        }

                                        if (currentPosition == 0 && measuredLength > 0)
                                        {
                                            currentPosition = measuredLength;
                                        }

                                        breakFound = true;

                                        break;
                                    }

                                    //We overflowed so we use the last available wrap position.
                                    currentPosition = lastWrapPosition == 0 ? measuredLength : lastWrapPosition;

                                    breakFound = true;

                                    break;
                                }

                                if (lineBreak.PositionMeasure != lineBreak.PositionWrap || lineBreak.PositionWrap != currentRunLength)
                                {
                                    lastWrapPosition = currentLength + lineBreak.PositionWrap;
                                }
                            }

                            break;
                        }
                }

                if (!breakFound)
                {
                    currentLength += currentRunLength;

                    continue;
                }

                measuredLength = currentPosition;

                break;
            }

            var (preSplitRuns, postSplitRuns) = SplitTextRuns(textRuns, measuredLength, objectPool, out var splitLength);

            try
            {
                TextLineBreak? textLineBreak;
                if (postSplitRuns?.Count > 0)
                {
                    List<TextRun> remainingRuns;
                    var postSplitCount = postSplitRuns.Count;

                    // reuse the list as much as possible:
                    // if canReuseTextRunList == true it's coming from previous remaining runs
                    if (canReuseTextRunList)
                    {
                        remainingRuns = textRuns;
                        remainingRuns.Clear();
                        // ensure capacity up front so List<T>.Add does not resize
                        // mid-loop (each resize is an Array.Copy of the backing array).
                        if (remainingRuns.Capacity < postSplitCount)
                        {
                            remainingRuns.Capacity = postSplitCount;
                        }
                    }
                    else
                    {
                        remainingRuns = new List<TextRun>(postSplitCount);
                    }

                    for (var i = 0; i < postSplitCount; ++i)
                    {
                        remainingRuns.Add(postSplitRuns[i]);
                    }

                    textLineBreak = new WrappingTextLineBreak(null, resolvedFlowDirection, remainingRuns);
                }
                else if (currentLineBreak?.TextEndOfLine is { } textEndOfLine)
                {
                    textLineBreak = new TextLineBreak(textEndOfLine, resolvedFlowDirection);
                }
                else
                {
                    textLineBreak = null;
                }

                if(preSplitRuns is null)
                {
                    return CreateEmptyTextLine(firstTextSourceIndex, paragraphWidth, paragraphProperties);
                }

                if (postSplitRuns?.Count > 0)
                {
                    ResetTrailingWhitespaceBidiLevels(preSplitRuns, paragraphProperties.FlowDirection, objectPool);
                }

                // SplitTextRuns has already computed the actual length of the first
                // segment (the cluster boundary may land slightly off the requested
                // length), so we just need to materialise the run array for TextLineImpl
                // no second-pass length sum required.
                var preSplitCount = preSplitRuns.Count;
                var remainingTextRuns = new TextRun[preSplitCount];

                for (var i = 0; i < preSplitCount; i++)
                {
                    remainingTextRuns[i] = preSplitRuns[i];
                }

                var textLine = new TextLineImpl(remainingTextRuns, firstTextSourceIndex, splitLength,
                    paragraphWidth, paragraphProperties, resolvedFlowDirection,
                    textLineBreak);

                textLine.FinalizeLine();

                return textLine;
            }
            finally
            {
                objectPool.TextRunLists.Return(ref preSplitRuns);
                objectPool.TextRunLists.Return(ref postSplitRuns);
            }
        }

        private static void ResetTrailingWhitespaceBidiLevels(RentedList<TextRun> lineTextRuns, FlowDirection paragraphFlowDirection, FormattingObjectPool objectPool)
        {
            if (lineTextRuns.Count == 0)
            {
                return;
            }

            var lastTextRunIndex = lineTextRuns.Count - 1;

            var lastTextRun = lineTextRuns[lastTextRunIndex];

            if (lastTextRun is not ShapedTextRun shapedText)
            {
                return;
            }

            var paragraphEmbeddingLevel = (sbyte)paragraphFlowDirection;

            if (shapedText.BidiLevel == paragraphEmbeddingLevel)
            {
                return;
            }

            var trailingWhitespaceLength = shapedText.GlyphRun.Metrics.TrailingWhitespaceLength;

            if (trailingWhitespaceLength == 0)
            {
                return;
            }

            var splitIndex = shapedText.Length - trailingWhitespaceLength;

            var (textRuns, trailingWhitespaceRuns) = SplitTextRuns([shapedText], splitIndex, objectPool);

            try
            {
                if (trailingWhitespaceRuns != null)
                {
                    for (var i = 0; i < trailingWhitespaceRuns.Count; i++)
                    {
                        if (trailingWhitespaceRuns[i] is ShapedTextRun shapedTextRun)
                        {
                            var newBuffer = shapedTextRun.ShapedBuffer.WithBidiLevel(paragraphEmbeddingLevel);

                            if (!ReferenceEquals(newBuffer, shapedTextRun.ShapedBuffer))
                            {
                                trailingWhitespaceRuns[i] = new ShapedTextRun(newBuffer, shapedTextRun.Properties);
                                shapedTextRun.Dispose();
                            }
                        }
                    }

                    lineTextRuns.RemoveAt(lastTextRunIndex);

                    if(textRuns is not null)
                    {
                        lineTextRuns.AddRange(textRuns);
                    }

                    lineTextRuns.AddRange(trailingWhitespaceRuns);
                }
            }
            finally
            {
                objectPool.TextRunLists.Return(ref textRuns);
                objectPool.TextRunLists.Return(ref trailingWhitespaceRuns);
            }
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
    }
}
