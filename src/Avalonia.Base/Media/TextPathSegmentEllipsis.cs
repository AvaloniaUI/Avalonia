using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media.TextFormatting;
using Avalonia.Utilities;

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

            if (MathUtilities.LessThan(Width, shapedSymbol.Size.Width))
            {
                // Nothing to collapse
                return null;
            }

            double totalWidth = textLine.Width;

            if (MathUtilities.LessThanOrClose(totalWidth, Width))
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

                // Pre-compute cumulative run start char positions so that
                // MeasureSegmentWidth can binary-search to the first overlapping
                // run instead of re-scanning from index 0 on every call. Built
                // once per Collapse; reused by every segment-width measurement.
                // runStartChars[i] = sum of lengths of runs 0..i-1;
                // runStartChars[Count] = total char length (sentinel).
                var runStartChars = new int[logicalRuns.Count + 1];
                for (var i = 0; i < logicalRuns.Count; i++)
                {
                    runStartChars[i + 1] = runStartChars[i] + logicalRuns[i].Length;
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
                                    var segmentWidth = MeasureSegmentWidth(logicalRuns, runStartChars, currentSegStart, globalIndex - currentSegStart);

                                    segments.Add((currentSegStart, globalIndex - currentSegStart, segmentWidth, false));
                                }

                                var separatorWidth = MeasureSegmentWidth(logicalRuns, runStartChars, globalIndex, 1);

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
                    var segmentWidth = MeasureSegmentWidth(logicalRuns, runStartChars, currentSegStart, globalIndex - currentSegStart);

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

                            if (MathUtilities.LessThanOrClose(totalWidth - trimmedWidth + shapedSymbol.Size.Width, Width))
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

                    if (segmentIndex < segments.Count - 1 && MathUtilities.GreaterThan(remainingWidth - segment.Width, Width))
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

                                    if (splitAt > 0)
                                    {
                                        (_, trimmedRun) = shapedRun.Split(splitAt);
                                    }
                                    else if (length > 0)
                                    {
                                        // The whole run fits in the remaining budget — no split needed,
                                        // use the run as-is. (Calling Split(0) throws.)
                                        trimmedRun = shapedRun;
                                    }
                                    // else: length == 0 → nothing of this run survives; trimmedRun stays null.
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
        /// <remarks>
        /// Uses the pre-computed <paramref name="runStartChars"/> cumulative-offset table
        /// to binary-search the first overlapping run (O(log N)) instead of re-scanning all
        /// runs from index 0 on every call. For each shaped overlap, delegates to
        /// <see cref="ShapedBuffer.GetCharRangeWidth"/>, which uses the cluster-width cache —
        /// O(log clusters) per call and direction-agnostic (the cache is built in logical
        /// order for both LTR and RTL buffers). Drawable runs are measured as a whole if
        /// they fully overlap the segment, matching the original behavior.
        /// </remarks>
        /// <param name="runs">The collection of text runs to measure.</param>
        /// <param name="runStartChars">Cumulative char-offset table; entry <c>i</c> is the
        /// total length of runs <c>0..i-1</c>, entry <c>Count</c> is the total char length.</param>
        /// <param name="segmentStart">Zero-based start index of the segment, relative to the combined text runs.</param>
        /// <param name="segmentLength">Number of characters in the segment. Must be non-negative.</param>
        /// <returns>The segment width in device-independent units, or 0 if the segment is empty or out of range.</returns>
        private static double MeasureSegmentWidth(IReadOnlyList<TextRun> runs, int[] runStartChars, int segmentStart, int segmentLength)
        {
            if (segmentLength <= 0)
            {
                return 0.0;
            }

            var segmentEnd = segmentStart + segmentLength;

            // Binary search runStartChars for the largest i with runStartChars[i] <= segmentStart.
            // That's the first run whose range can overlap the segment.
            var i = FindFirstOverlappingRun(runStartChars, segmentStart);

            double width = 0.0;

            for (; i < runs.Count; i++)
            {
                var runStart = runStartChars[i];
                if (runStart >= segmentEnd)
                {
                    break;
                }

                var run = runs[i];
                var runEnd = runStart + run.Length;

                var overlapStart = Math.Max(segmentStart, runStart);
                var overlapEnd = Math.Min(segmentEnd, runEnd);
                if (overlapEnd <= overlapStart)
                {
                    continue;
                }

                switch (run)
                {
                    case ShapedTextRun shaped:
                        {
                            // ShapedBuffer.GetCharRangeWidth uses the cluster cache; O(log clusters).
                            width += shaped.ShapedBuffer.GetCharRangeWidth(overlapStart - runStart, overlapEnd - runStart);
                            break;
                        }
                    case DrawableTextRun d:
                        {
                            // Drawables are atomic: count full width when they completely overlap.
                            if (overlapEnd - overlapStart >= d.Length)
                            {
                                width += d.Size.Width;
                            }
                            break;
                        }
                }
            }

            return width;
        }

        /// <summary>
        /// Binary-search <paramref name="runStartChars"/> for the largest index
        /// <c>i</c> such that <c>runStartChars[i] &lt;= charIndex</c>. That index
        /// is the first run that can contain or precede <paramref name="charIndex"/>.
        /// </summary>
        private static int FindFirstOverlappingRun(int[] runStartChars, int charIndex)
        {
            if (charIndex <= 0)
            {
                return 0;
            }

            var lo = 0;
            // Upper bound excludes the sentinel entry; we want a run index, not a boundary.
            var hi = runStartChars.Length - 2;
            if (hi < 0)
            {
                return 0;
            }

            while (lo < hi)
            {
                var mid = (lo + hi + 1) >> 1;
                if (runStartChars[mid] <= charIndex)
                {
                    lo = mid;
                }
                else
                {
                    hi = mid - 1;
                }
            }
            return lo;
        }
    }
}
