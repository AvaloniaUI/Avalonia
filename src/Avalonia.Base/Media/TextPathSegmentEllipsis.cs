using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media.TextFormatting;

namespace Avalonia.Media
{
    /// <summary>
    /// Provides text collapsing properties that replace the middle segments of a file path with an ellipsis symbol when
    /// the rendered width exceeds a specified limit.
    /// </summary>
    /// <remarks>This class is typically used to display file paths in a compact form by collapsing segments
    /// near the center and inserting an ellipsis, ensuring that the most significant parts of the path remain visible.
    /// </remarks>
    public sealed class TextPathSegmentEllipsis : TextCollapsingProperties
    {
        private readonly char[] _separators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, '/', '\\' };

        /// <summary>
        /// Initializes a new instance of the TextPathSegmentEllipsis class that represents an ellipsis segment in a
        /// text path with the specified symbol, width, text formatting properties, and flow direction.
        /// </summary>
        /// <param name="ellipsis">The string to use as the ellipsis symbol in the text path segment. Cannot be null.</param>
        /// <param name="width">The width.</param>
        /// <param name="textRunProperties">The text formatting properties to apply to the ellipsis symbol. Cannot be null.</param>
        /// <param name="flowDirection">The flow direction for rendering the ellipsis segment. Specifies whether text flows left-to-right or
        /// right-to-left.</param>
        public TextPathSegmentEllipsis(string ellipsis, double width, TextRunProperties textRunProperties, FlowDirection flowDirection)
        {
            Width = width;
            Symbol = new TextCharacters(ellipsis, textRunProperties);
            FlowDirection = flowDirection;
        }

        public override double Width { get; }

        public override TextRun Symbol { get; }

        public override FlowDirection FlowDirection { get; }

        public override TextRun[]? Collapse(TextLine textLine)
        {
            if (textLine.TextRuns.Count == 0)
            {
                return null;
            }

            var objectPool = FormattingObjectPool.Instance;

            var shapedSymbol = TextFormatter.CreateSymbol(Symbol, FlowDirection);

            if (Width < shapedSymbol.Size.Width)
            {
                // Nothing to collapse
                return null;
            }

            double totalWidth = textLine.Width;

            if (totalWidth <= Width)
            {
                // Nothing to collapse
                return null;
            }

            // Extract logical runs from the line
            FormattingObjectPool.RentedList<TextRun>? logicalRuns = null;

            try
            {
                logicalRuns = objectPool.TextRunLists.Rent();

                var enumerator = new LogicalTextRunEnumerator(textLine);

                while (enumerator.MoveNext(out var r))
                {
                    logicalRuns.Add(r);
                }

                // Segment ranges
                var segments = new List<(int Start, int Length, double Width, bool IsSeparator)>();
                var candidateSegmentIndices = new List<int>();
                var globalIndex = 0;
                var currentSegStart = 0;
                var inSeparator = false;

                for (var i = 0; i < logicalRuns.Count; i++)
                {
                    var run = logicalRuns[i];

                    if (run is ShapedTextRun shaped)
                    {
                        var span = shaped.Text.Span;
                        var localPos = 0;

                        while (localPos < span.Length)
                        {
                            var ch = span[localPos];

                            var isSep = IsSeparator(ch);

                            if (isSep)
                            {
                                // finish previous non-separator segment
                                if (!inSeparator && globalIndex - currentSegStart > 0)
                                {
                                    var segmentWidth = TextPathSegmentEllipsis.MeasureSegmentWidth(logicalRuns, currentSegStart, globalIndex - currentSegStart);

                                    segments.Add((currentSegStart, globalIndex - currentSegStart, segmentWidth, false));
                                }

                                var separatorWidth = TextPathSegmentEllipsis.MeasureSegmentWidth(logicalRuns, globalIndex, 1);

                                // separator as its own segment
                                segments.Add((globalIndex, 1, separatorWidth, true));

                                // next segment starts after separator
                                currentSegStart = globalIndex + 1;
                                inSeparator = true;
                            }
                            else
                            {
                                if (inSeparator)
                                {
                                    // start of a non-separator segment
                                    currentSegStart = globalIndex;
                                    inSeparator = false;
                                }
                            }

                            localPos++;
                            globalIndex++;
                        }
                    }
                    else
                    {
                        // Non shaped run is treated as non-separator
                        if (inSeparator)
                        {
                            currentSegStart = globalIndex;
                            inSeparator = false;
                        }

                        globalIndex += run.Length;
                    }
                }

                // Add last pending segment if any
                if (globalIndex - currentSegStart > 0)
                {
                    var segmentWidth = TextPathSegmentEllipsis.MeasureSegmentWidth(logicalRuns, currentSegStart, globalIndex - currentSegStart);

                    segments.Add((currentSegStart, globalIndex - currentSegStart, segmentWidth, false));
                }

                if (segments.Count == 0)
                {
                    // Nothing to collapse
                    return null;
                }

                var prefix = new double[segments.Count + 1];

                // Measure segment widths 
                for (int i = 0; i < segments.Count; i++)
                {
                    var (Start, Length, SegmentWidth, IsSeparator) = segments[i];

                    if (!IsSeparator)
                    {
                        candidateSegmentIndices.Add(i);
                    }

                    prefix[i + 1] = prefix[i] + SegmentWidth;
                }

                // Determine center character index to prefer collapsing ranges near the middle.
                var midChar = globalIndex / 2;

                // Find candidate whose center is closest to midChar
                int centerCandidateIdx = 0;
                long bestDist = long.MaxValue;

                for (int i = 0; i < candidateSegmentIndices.Count; ++i)
                {
                    var (Start, Length, SegmentWidth, IsSeparator) = segments[candidateSegmentIndices[i]];
                    var segCenter = Start + Length / 2;
                    var dist = Math.Abs(segCenter - midChar);

                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        centerCandidateIdx = i;
                    }
                }

                // Expand windows around centerCandidateIdx, up to _maxCollapsedSegments segments.
                var candidateCount = candidateSegmentIndices.Count;

                if (candidateCount > 0)
                {
                    for (int windowSize = 1; windowSize <= candidateCount; windowSize++)
                    {
                        // For a given windowSize, try all windows of that size centered as close as possible to centerCandidateIdx.
                        // Compute start index of window such that center is as near as possible.
                        int half = (windowSize - 1) / 2;
                        int start = centerCandidateIdx - half;
                        // For even window sizes, prefer left-leaning start, also try shifting the window across the center.
                        var windowStarts = new List<int>();

                        // clamp start range
                        int minStart = Math.Max(0, centerCandidateIdx - (windowSize - 1));
                        int maxStart = Math.Min(candidateCount - windowSize, centerCandidateIdx + (windowSize - 1));

                        // Left side first
                        for (int s = start; s >= minStart; s--)
                        {
                            windowStarts.Add(s);
                        }

                        // Right side next
                        for (int s = start + 1; s <= maxStart; s++)
                        {
                            windowStarts.Add(s);
                        }

                        foreach (var windowStart in windowStarts)
                        {
                            if (windowStart < 0 || windowStart + windowSize > candidateCount)
                            {
                                continue;
                            }

                            int leftCand = windowStart;
                            int rightCand = windowStart + windowSize - 1;

                            // Map candidate window to segments range (in segments list)
                            int segStartIndex = candidateSegmentIndices[leftCand];
                            int segEndIndex = candidateSegmentIndices[rightCand];

                            // Ensure that we leave at least one character on each side (prefer middle-only removal)
                            var leftRemaining = segments[segStartIndex].Start;
                            var rightRemaining = globalIndex - (segments[segEndIndex].Start + segments[segEndIndex].Length);

                            if (leftRemaining <= 0 || rightRemaining <= 0)
                            {
                                continue;
                            }

                            var trimmedWidth = prefix[segEndIndex + 1] - prefix[segStartIndex];

                            if (totalWidth - trimmedWidth + shapedSymbol.Size.Width <= Width)
                            {
                                // perform split using character indices
                                var removeStart = segments[segStartIndex].Start;
                                var removeLength = (segments[segEndIndex].Start + segments[segEndIndex].Length) - removeStart;

                                FormattingObjectPool.RentedList<TextRun>? first = null;
                                FormattingObjectPool.RentedList<TextRun>? remainder = null;
                                FormattingObjectPool.RentedList<TextRun>? middle = null;
                                FormattingObjectPool.RentedList<TextRun>? last = null;

                                try
                                {
                                    (first, remainder) = TextFormatterImpl.SplitTextRuns(logicalRuns, removeStart, objectPool);

                                    if (remainder == null)
                                    {
                                        // We reached the end
                                        return null;
                                    }

                                    (middle, last) = TextFormatterImpl.SplitTextRuns(remainder, removeLength, objectPool);

                                    // Build resulting runs
                                    // first + shapedSymbol + last
                                    var result = new TextRun[(first?.Count ?? 0) + 1 + (last?.Count ?? 0)];
                                    var index = 0;

                                    if (first != null)
                                    {
                                        foreach (var run in first)
                                        {
                                            result[index++] = run;
                                        }
                                    }

                                    result[index++] = shapedSymbol;

                                    if (last != null)
                                    {
                                        foreach (var run in last)
                                        {
                                            result[index++] = run;
                                        }
                                    }

                                    return result;
                                }
                                finally
                                {
                                    // Return rented lists
                                    objectPool.TextRunLists.Return(ref first);
                                    objectPool.TextRunLists.Return(ref remainder);
                                    objectPool.TextRunLists.Return(ref middle);
                                    objectPool.TextRunLists.Return(ref last);
                                }
                            }
                        }
                    }
                }

                // Fallback - try to trim at segment boundaries from start
                var currentLength = 0;
                var remainingWidth = textLine.WidthIncludingTrailingWhitespace;

                for (var segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
                {
                    var segment = segments[segmentIndex];

                    if (segmentIndex < segments.Count - 1 && remainingWidth - segment.Width > Width)
                    {
                        remainingWidth -= segment.Width;
                        currentLength += segment.Length;

                        continue;
                    }

                    FormattingObjectPool.RentedList<TextRun>? first = null;
                    FormattingObjectPool.RentedList<TextRun>? second = null;

                    try
                    {
                        // Split before current segment
                        (first, second) = TextFormatterImpl.SplitTextRuns(logicalRuns, currentLength, objectPool);

                        TextRun? trimmedRun = null;
                        var remainingRunCount = 0;

                        if (second != null && second.Count > 0)
                        {
                            remainingRunCount = Math.Max(0, second.Count - 1);

                            var run = second[0];

                            if (run is ShapedTextRun shapedRun)
                            {
                                var measureWidth = Width - shapedSymbol.Size.Width;

                                if (shapedRun.TryMeasureCharactersBackwards(measureWidth, out var length, out _))
                                {
                                    var splitAt = shapedRun.Length - length;

                                    (_, trimmedRun) = shapedRun.Split(splitAt);
                                }
                            }
                        }

                        var runCount = (trimmedRun != null ? 1 : 0) + 1 + remainingRunCount;

                        var result = new TextRun[runCount];
                        var index = 0;

                        // Append symbol
                        result[index++] = shapedSymbol;

                        // Append trimmed run if any
                        if (trimmedRun != null)
                        {
                            result[index++] = trimmedRun;
                        }

                        // Append remaining runs
                        if (second != null)
                        {
                            for(var i = 1; i < second.Count; i++)
                            {
                                var run = second[i];

                                result[index++] = run;
                            }
                        }

                        return result;

                    }
                    finally
                    {
                        // Return rented lists
                        objectPool.TextRunLists.Return(ref first);
                        objectPool.TextRunLists.Return(ref second);
                    }
                }

                // No suitable segment found
                return null;
            }
            finally
            {
                objectPool.TextRunLists.Return(ref logicalRuns);
            }
        }

        private bool IsSeparator(char ch)
        {
            foreach (var s in _separators)
            {
                if (s == ch)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Calculates the total width of a specified segment within a sequence of text runs.
        /// </summary>
        /// <remarks>The method accounts for partial overlaps between the segment and individual text
        /// runs. Drawable runs are measured as a whole if any part overlaps the segment.</remarks>
        /// <param name="runs">The collection of text runs to measure. Each run represents a contiguous sequence of formatted text.</param>
        /// <param name="segmentStart">The zero-based index of the first character in the segment to measure, relative to the combined text runs.</param>
        /// <param name="segmentLength">The number of characters in the segment to measure. Must be non-negative.</param>
        /// <returns>The total width, in device-independent units, of the specified text segment. Returns 0.0 if the segment is
        /// empty or does not overlap any runs.</returns>
        private static double MeasureSegmentWidth(IReadOnlyList<TextRun> runs, int segmentStart, int segmentLength)
        {
            // segment range in global character indices
            var segmentEnd = segmentStart + segmentLength;
            var currentChar = 0;
            double width = 0.0;

            for (var i = 0; i < runs.Count; i++)
            {
                var run = runs[i];
                var runStart = currentChar;
                var runEnd = runStart + run.Length;

                // no overlap with requested segment
                if (runEnd <= segmentStart)
                {
                    currentChar = runEnd;
                    continue;
                }

                if (runStart >= segmentEnd)
                {
                    break;
                }

                // overlap range within this run [overlapStart, overlapEnd)
                var overlapStart = Math.Max(segmentStart, runStart);
                var overlapEnd = Math.Min(segmentEnd, runEnd);
                var overlapLen = overlapEnd - overlapStart;

                if (overlapLen <= 0)
                {
                    currentChar = runEnd;
                    continue;
                }

                switch (run)
                {
                    case ShapedTextRun shaped:
                        {
                            var buffer = shaped.ShapedBuffer;
                            if (buffer.Length == 0)
                            {
                                break;
                            }

                            // local char offsets inside this run
                            var localStart = overlapStart - runStart;
                            var localEnd = overlapEnd - runStart;

                            // base cluster used by this buffer (see ShapedBuffer.Split logic)
                            var baseCluster = buffer[0].GlyphCluster;

                            // glyph clusters are increasing — stop once we passed localEnd
                            for (var gi = 0; gi < buffer.Length; gi++)
                            {
                                var g = buffer[gi];
                                var clusterLocal = g.GlyphCluster - baseCluster;

                                if (clusterLocal < localStart)
                                    continue;

                                if (clusterLocal >= localEnd)
                                    break;

                                width += g.GlyphAdvance;
                            }

                            break;
                        }
                    case DrawableTextRun d:
                        {
                            // For drawable runs, count full width if they completely overlap
                            if (overlapLen >= d.Length)
                            {
                                width += d.Size.Width;
                            }
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }

                currentChar = runEnd;
            }

            return width;
        }
    }
}
