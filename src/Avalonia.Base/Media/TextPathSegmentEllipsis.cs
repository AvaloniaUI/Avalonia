using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media.TextFormatting;

namespace Avalonia.Media
{
    /// <summary>
    /// Collapsing properties that attempt to collapse a file path by replacing
    /// the smallest path segment (between separators) with an ellipsis so the
    /// whole path fits into the available width.
    /// </summary>
    public sealed class TextPathSegmentEllipsis : TextCollapsingProperties
    {
        private readonly char[] _separators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, '/', '\\' };

        private readonly int _maxCollapsedSegments;

        public TextPathSegmentEllipsis(string ellipsis, double width, TextRunProperties textRunProperties, FlowDirection flowDirection, int maxCollapsedSegments = int.MaxValue)
        {
            Width = width;
            Symbol = new TextCharacters(ellipsis, textRunProperties);
            FlowDirection = flowDirection;
            _maxCollapsedSegments = maxCollapsedSegments <= 0 ? int.MaxValue : maxCollapsedSegments;
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

            var shapedSymbol = TextFormatter.CreateSymbol(Symbol, FlowDirection.LeftToRight);

            if (Width < shapedSymbol.GlyphRun.Bounds.Width)
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
                var segments = new List<(int Start, int Length, bool IsSeperator)>();
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
                                    segments.Add((currentSegStart, globalIndex - currentSegStart, false));
                                }

                                // separator as its own segment
                                segments.Add((globalIndex, 1, true));

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
                    segments.Add((currentSegStart, globalIndex - currentSegStart, false));
                }

                if (segments.Count == 0)
                {
                    // Nothing to collapse
                    return null;
                }

                // Compute width for every segment once and prefix sums for O(1) range width.
                var segmentWidths = new double[segments.Count];
                var candidateSegmentIndices = new List<int>();
                var smallestWidth = double.MaxValue;
                var prefix = new double[segmentWidths.Length + 1];

                // Measure segment widths and track smallest non-separator segment width
                for (int i = 0; i < segments.Count; i++)
                {
                    var (Start, Length, IsSeperator) = segments[i];

                    var segmentWidth = TextPathSegmentEllipsis.MeasureSegmentWidth(logicalRuns, Start, Length);

                    if (!IsSeperator)
                    {
                        if (segmentWidth < smallestWidth)
                        {
                            smallestWidth = segmentWidth;
                        }

                        candidateSegmentIndices.Add(i);
                    }

                    segmentWidths[i] = segmentWidth;

                    prefix[i + 1] = prefix[i] + segmentWidth;
                }

                if (candidateSegmentIndices.Count == 0)
                {
                    // Nothing to collapse
                    return null;
                }

                // Determine center character index to prefer collapsing ranges near the middle.
                var midChar = globalIndex / 2;

                // Find candidate whose center is closest to midChar
                int centerCandidateIdx = 0;
                long bestDist = long.MaxValue;

                for (int i = 0; i < candidateSegmentIndices.Count; ++i)
                {
                    var (Start, Length, IsSeperator) = segments[candidateSegmentIndices[i]];
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
                var maxSegmentsToTry = Math.Min(_maxCollapsedSegments, candidateCount);

                for (int windowSize = 1; windowSize <= maxSegmentsToTry; windowSize++)
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

                    foreach (var ws in windowStarts)
                    {
                        if (ws < 0 || ws + windowSize > candidateCount)
                        {
                            continue;
                        }

                        int leftCand = ws;
                        int rightCand = ws + windowSize - 1;

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

                        // removed width = prefix[segEndIndex+1] - prefix[segStartIndex]
                        var removedWidth = prefix[segEndIndex + 1] - prefix[segStartIndex];

                        var newTotal = totalWidth - removedWidth + shapedSymbol.Size.Width;

                        if (newTotal <= Width)
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
                                var idx = 0;

                                if (first != null)
                                {
                                    foreach (var run in first)
                                    {
                                        result[idx++] = run;
                                    }
                                }

                                result[idx++] = shapedSymbol;

                                if (last != null)
                                {
                                    foreach (var run in last)
                                    {
                                        result[idx++] = run;
                                    }
                                }

                                return result;
                            }
                            finally
                            {
                                objectPool.TextRunLists.Return(ref first);
                                objectPool.TextRunLists.Return(ref remainder);
                                objectPool.TextRunLists.Return(ref middle);
                                objectPool.TextRunLists.Return(ref last);
                            }
                        }
                    }
                }

                // Fallback
                // Try single non-separator segments (smallest first)
                var singleCandidates = new List<(int SegIndex, double Width)>(candidateSegmentIndices.Count);

                foreach (var si in candidateSegmentIndices)
                {
                    var segmentWidth = segmentWidths[si];

                    singleCandidates.Add((si, segmentWidth));
                }

                // Sort candidates by width (smallest first)
                singleCandidates.Sort((a, b) => a.Width.CompareTo(b.Width));

                // Ensure the original last segment is tried last: if it's a candidate, move it to the end.
                var originalLastSegmentIndex = segments.Count - 1;
                var moveIdx = singleCandidates.FindIndex(x => x.SegIndex == originalLastSegmentIndex);
                if (moveIdx >= 0 && moveIdx != singleCandidates.Count - 1)
                {
                    var lastEntry = singleCandidates[moveIdx];
                    singleCandidates.RemoveAt(moveIdx);
                    singleCandidates.Add(lastEntry);
                }

                foreach (var cand in singleCandidates)
                {
                    var seg = segments[cand.SegIndex];
                    var removedWidth = cand.Width;
                    var newTotal = totalWidth - removedWidth + shapedSymbol.Size.Width;

                    if (newTotal <= Width)
                    {
                        var removeStart = seg.Start;
                        var removeLength = seg.Length;

                        FormattingObjectPool.RentedList<TextRun>? first = null;
                        FormattingObjectPool.RentedList<TextRun>? remainder = null;
                        FormattingObjectPool.RentedList<TextRun>? middle = null;
                        FormattingObjectPool.RentedList<TextRun>? last = null;

                        try
                        {
                            (first, remainder) = TextFormatterImpl.SplitTextRuns(logicalRuns, removeStart, objectPool);

                            if (remainder == null)
                            {
                                // Nothing to collapse
                                return null;
                            }

                            (middle, last) = TextFormatterImpl.SplitTextRuns(remainder, removeLength, objectPool);

                            var count = (first?.Count ?? 0) + 1 + (last?.Count ?? 0);

                            var isLastSegmentTrimmed = cand.SegIndex == originalLastSegmentIndex;

                            // Special case: if the last segment is trimmed but has larger width than available, add an extra run for the trimmed last segment
                            if (isLastSegmentTrimmed)
                            {
                                count++; // extra run for trimmed last segment
                            }

                            // Build resulting runs: first + shapedSymbol + last
                            var result = new TextRun[count];
                            var idx = 0;

                            if (first != null)
                            {
                                foreach (var run in first)
                                {
                                    result[idx++] = run;
                                }
                            }

                            result[idx++] = shapedSymbol;

                            if (isLastSegmentTrimmed)
                            {
                                var (RunIndex, Offset) = FindRunAtCharacterIndex(logicalRuns, removeStart);

                                for (var i = logicalRuns.Count - 1; i >= RunIndex; i--)
                                {
                                    var shapedTextRun = logicalRuns[i] as ShapedTextRun;

                                    if (shapedTextRun is null)
                                    {
                                        continue;
                                    }

                                    var measureWidth = Width - shapedSymbol.Size.Width - newTotal;

                                    if (shapedTextRun.TryMeasureCharactersBackwards(measureWidth, out var length, out _))
                                    {
                                        if (length > 0)
                                        {
                                            var (_, remaining) = shapedTextRun.Split(shapedTextRun.Length - length);

                                            if (remaining is not null)
                                            {
                                                result[idx++] = remaining;
                                            }
                                        } 
                                    }
                                }
                            }

                            if (last != null)
                            {
                                foreach (var run in last)
                                {
                                    result[idx++] = run;
                                }
                            }

                            return result;
                        }
                        finally
                        {
                            objectPool.TextRunLists.Return(ref first);
                            objectPool.TextRunLists.Return(ref remainder);
                            objectPool.TextRunLists.Return(ref middle);
                            objectPool.TextRunLists.Return(ref last);
                        }
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
        /// Finds the index of the text run and the offset within that run corresponding to the specified character
        /// index.
        /// </summary>
        /// <remarks>If the specified character index does not fall within any run, the method returns
        /// (-1, -1). This method does not validate whether the character index is within the bounds of the combined
        /// runs; callers should ensure valid input.</remarks>
        /// <param name="runs">A read-only list of text runs to search. Each run represents a contiguous segment of text.</param>
        /// <param name="characterIndex">The zero-based character index to locate within the combined text runs. Must be greater than or equal to
        /// zero and less than the total length of all runs.</param>
        /// <returns>A tuple containing the index of the run and the offset within that run for the specified character index.
        /// Returns (-1, -1) if the character index is out of range.</returns>
        private static (int RunIndex, int Offset) FindRunAtCharacterIndex(IReadOnlyList<TextRun> runs, int characterIndex)
        {
            var current = 0;

            for (var i = 0; i < runs.Count; i++)
            {
                var run = runs[i];

                if (characterIndex < current + run.Length)
                {
                    var offset = characterIndex - current;

                    return (i, offset);
                }

                current += run.Length;
            }

            return (-1, -1);
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
                            // A drawable run can't be split; if any overlap exists, count its full width.
                            if (overlapLen > 0)
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
