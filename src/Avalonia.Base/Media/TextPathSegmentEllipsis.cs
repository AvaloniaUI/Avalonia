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
            var objectPool = FormattingObjectPool.Instance;

            var shapedSymbol = TextFormatter.CreateSymbol(Symbol, FlowDirection.LeftToRight);

            if (Width < shapedSymbol.GlyphRun.Bounds.Width)
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

                if (logicalRuns.Count == 0)
                {
                    // Nothing to collapse
                    return null;
                }

                double totalWidth = 0;

                foreach (var run in logicalRuns)
                {
                    switch (run)
                    {
                        case ShapedTextRun shaped:
                            totalWidth += shaped.Size.Width;
                            break;
                        case DrawableTextRun d:
                            totalWidth += d.Size.Width;
                            break;
                    }
                }

                if (totalWidth <= Width)
                {
                    // Nothing to collapse
                    return null;
                }

                // Segment ranges
                var segments = new List<(int Start, int Length)>();
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
                                    segments.Add((currentSegStart, globalIndex - currentSegStart));
                                }

                                // separator as its own segment
                                segments.Add((globalIndex, 1));

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
                    segments.Add((currentSegStart, globalIndex - currentSegStart));
                }

                if (segments.Count == 0)
                {
                    // Nothing to collapse
                    return null;
                }

                // Compute width for every segment once and prefix sums for O(1) range width.
                var segmentWidths = new double[segments.Count];

                for (int si = 0; si < segments.Count; ++si)
                {
                    var seg = segments[si];

                    segmentWidths[si] = TextPathSegmentEllipsis.MeasureSegmentWidth(logicalRuns, seg.Start, seg.Length, objectPool);
                }

                var prefix = new double[segmentWidths.Length + 1];

                for (int i = 0; i < segmentWidths.Length; i++)
                {
                    prefix[i + 1] = prefix[i] + segmentWidths[i];
                }   

                // Collect indices of non-separator segments (candidate segments).
                var candidateSegmentIndices = new List<int>();

                for (int si = 0; si < segments.Count; ++si)
                {
                    var seg = segments[si];

                    if (seg.Length <= 0)
                    {
                        continue;
                    }

                    // Check the character at seg.Start: if it's a separator, skip
                    var (foundRunIndex, foundRunOffset) = FindRunAtCharacterIndex(logicalRuns, seg.Start);
                    var isSeparatorSegment = false;

                    if (foundRunIndex >= 0)
                    {
                        var runAt = logicalRuns[foundRunIndex] as ShapedTextRun;

                        if (runAt != null && foundRunOffset < runAt.Text.Span.Length)
                        {
                            var ch = runAt.Text.Span[foundRunOffset];

                            if (IsSeparator(ch))
                            {
                                isSeparatorSegment = true;
                            }                               
                        }
                    }

                    if (!isSeparatorSegment)
                    {
                        candidateSegmentIndices.Add(si);
                    }
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

                for (int ci = 0; ci < candidateSegmentIndices.Count; ++ci)
                {
                    var seg = segments[candidateSegmentIndices[ci]];
                    var segCenter = seg.Start + seg.Length / 2;
                    var dist = Math.Abs(segCenter - midChar);

                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        centerCandidateIdx = ci;
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
                    singleCandidates.Add((si, segmentWidths[si]));
                }

                singleCandidates.Sort((a, b) => a.Width.CompareTo(b.Width));

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

                            // Build resulting runs: first + shapedSymbol + last
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

        private static double MeasureSegmentWidth(IReadOnlyList<TextRun> runs, int segmentStart, int segmentLength, FormattingObjectPool objectPool)
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
