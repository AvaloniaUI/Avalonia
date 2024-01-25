using System;
using System.Diagnostics;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Reorders text runs according to their bidi level.
    /// </summary>
    /// <remarks>To avoid allocations, this class is designed to be reused.</remarks>
    internal sealed class BidiReorderer
    {
        [ThreadStatic] private static BidiReorderer? t_instance;

        private ArrayBuilder<OrderedBidiRun> _runs;
        private ArrayBuilder<BidiRange> _ranges;

        public static BidiReorderer Instance
            => t_instance ??= new();

        public IndexedTextRun[] BidiReorder(Span<TextRun> textRuns, FlowDirection flowDirection, int firstTextSourceIndex)
        {
            Debug.Assert(_runs.Length == 0);
            Debug.Assert(_ranges.Length == 0);

            if (textRuns.IsEmpty)
            {
                return Array.Empty<IndexedTextRun>();
            }

            try
            {
                sbyte? previousLevel = null;

                _runs.Add(textRuns.Length);

                // Build up the collection of ordered runs.
                for (var i = 0; i < textRuns.Length; i++)
                {
                    var textRun = textRuns[i];

                    var orderedRun = new OrderedBidiRun(i, textRun, GetRunBidiLevel(textRun, flowDirection, previousLevel));

                    _runs[i] = orderedRun;

                    if (i > 0)
                    {
                        _runs[i - 1].NextRunIndex = i;
                    }

                    previousLevel = orderedRun.Level;
                }

                // Reorder them into visual order.
                var firstIndex = LinearReorder();
                var indexedTextRuns = new IndexedTextRun[textRuns.Length];

                for (var i = 0; i < textRuns.Length; i++)
                {
                    var currentRun = textRuns[i];

                    indexedTextRuns[i] = new IndexedTextRun
                    {
                        TextRun = currentRun,
                        TextSourceCharacterIndex = firstTextSourceIndex,
                        RunIndex = i,
                        NextRunIndex = i + 1
                    };

                    firstTextSourceIndex += currentRun.Length;
                }

                // Now perform a recursive reversal of each run.
                // From the highest level found in the text to the lowest odd level on each line, including intermediate levels
                // not actually present in the text, reverse any contiguous sequence of characters that are at that level or higher.
                // https://unicode.org/reports/tr9/#L2
                sbyte max = 0;
                var min = sbyte.MaxValue;

                for (var i = 0; i < textRuns.Length; i++)
                {
                    var level = _runs[i].Level;

                    if (level > max)
                    {
                        max = level;
                    }

                    if ((level & 1) != 0 && level < min)
                    {
                        min = level;
                    }
                }

                if (min > max)
                {
                    min = max;
                }

                if (max == 0 || (min == max && (max & 1) == 0))
                {
                    // Nothing to reverse.
                    return indexedTextRuns;
                }

                // Now apply the reversal and replace the original contents.
                var minLevelToReverse = max;
                int currentIndex;

                while (minLevelToReverse >= min)
                {
                    currentIndex = firstIndex;

                    while (currentIndex >= 0)
                    {
                        ref var current = ref _runs[currentIndex];
                        if (current.Level >= minLevelToReverse && current.Level % 2 != 0)
                        {
                            if (current.Run is ShapedTextRun { IsReversed: false } shapedTextCharacters)
                            {
                                shapedTextCharacters.Reverse();
                            }
                        }

                        currentIndex = current.NextRunIndex;
                    }

                    minLevelToReverse--;
                }

                var index = 0;

                currentIndex = firstIndex;

                while (currentIndex >= 0)
                {
                    ref var current = ref _runs[currentIndex];

                    textRuns[index] = current.Run;

                    var indexedRun = indexedTextRuns[index];

                    indexedRun.RunIndex = current.RunIndex;

                    indexedRun.NextRunIndex = current.NextRunIndex;

                    index++;

                    currentIndex = current.NextRunIndex;
                }

                return indexedTextRuns;
            }
            finally
            {
                FormattingBufferHelper.ClearThenResetIfTooLarge(ref _runs);
                FormattingBufferHelper.ClearThenResetIfTooLarge(ref _ranges);
            }
        }

        private static sbyte GetRunBidiLevel(TextRun run, FlowDirection flowDirection, sbyte? previousLevel)
        {
            if (run is ShapedTextRun shapedTextRun)
            {
                return shapedTextRun.BidiLevel;
            }

            var defaultLevel = (sbyte)(flowDirection == FlowDirection.LeftToRight ? 0 : 1);

            if (run is TextEndOfLine)
            {
                return defaultLevel;
            }

            if(previousLevel is not null)
            {
                return previousLevel.Value;
            }

            return defaultLevel;
        }

        /// <summary>
        /// Reorders the runs from logical to visual order.
        /// <see href="https://github.com/fribidi/linear-reorder/blob/f2f872257d4d8b8e137fcf831f254d6d4db79d3c/linear-reorder.c"/>
        /// </summary>
        /// <returns>The first run index in visual order.</returns>
        private int LinearReorder()
        {
            var runIndex = 0;
            var rangeIndex = -1;

            while (runIndex >= 0)
            {
                ref var run = ref _runs[runIndex];
                var nextRunIndex = run.NextRunIndex;

                while (rangeIndex >= 0
                    && _ranges[rangeIndex].Level > run.Level
                    && _ranges[rangeIndex].PreviousRangeIndex >= 0
                    && _ranges[_ranges[rangeIndex].PreviousRangeIndex].Level >= run.Level)
                {

                    rangeIndex = MergeRangeWithPrevious(rangeIndex);
                }

                if (rangeIndex >= 0 && _ranges[rangeIndex].Level >= run.Level)
                {
                    // Attach run to the range.
                    if ((run.Level & 1) != 0)
                    {
                        // Odd, range goes to the right of run.
                        run.NextRunIndex = _ranges[rangeIndex].LeftRunIndex;
                        _ranges[rangeIndex].LeftRunIndex = runIndex;
                    }
                    else
                    {
                        // Even, range goes to the left of run.
                        _runs[_ranges[rangeIndex].RightRunIndex].NextRunIndex = runIndex;
                        _ranges[rangeIndex].RightRunIndex = runIndex;
                    }

                    _ranges[rangeIndex].Level = run.Level;
                }
                else
                {
                    var r = new BidiRange(run.Level, runIndex, runIndex, previousRangeIndex: rangeIndex);
                    _ranges.AddItem(r);
                    rangeIndex = _ranges.Length - 1;
                }

                runIndex = nextRunIndex;
            }

            while (rangeIndex >= 0 && _ranges[rangeIndex].PreviousRangeIndex >= 0)
            {
                rangeIndex = MergeRangeWithPrevious(rangeIndex);
            }

            // Terminate.
            _runs[_ranges[rangeIndex].RightRunIndex].NextRunIndex = -1;

            return _runs[_ranges[rangeIndex].LeftRunIndex].RunIndex;
        }

        private int MergeRangeWithPrevious(int index)
        {
            var previousIndex = _ranges[index].PreviousRangeIndex;
            ref var previous = ref _ranges[previousIndex];

            int leftIndex;
            int rightIndex;

            if ((previous.Level & 1) != 0)
            {
                // Odd, previous goes to the right of range.
                leftIndex = index;
                rightIndex = previousIndex;
            }
            else
            {
                // Even, previous goes to the left of range.
                leftIndex = previousIndex;
                rightIndex = index;
            }

            // Stitch them
            ref var left = ref _ranges[leftIndex];
            ref var right = ref _ranges[rightIndex];
            _runs[left.RightRunIndex].NextRunIndex = _runs[right.LeftRunIndex].RunIndex;
            previous.LeftRunIndex = left.LeftRunIndex;
            previous.RightRunIndex = right.RightRunIndex;

            return previousIndex;
        }

        private struct BidiRange
        {
            public BidiRange(sbyte level, int leftRunIndex, int rightRunIndex, int previousRangeIndex)
            {
                Level = level;
                LeftRunIndex = leftRunIndex;
                RightRunIndex = rightRunIndex;
                PreviousRangeIndex = previousRangeIndex;
            }

            public sbyte Level { get; set; }

            public int LeftRunIndex { get; set; }

            public int RightRunIndex { get; set; }

            public int PreviousRangeIndex { get; } // -1 if none
        }
    }

    internal struct OrderedBidiRun
    {
        public OrderedBidiRun(int runIndex, TextRun run, sbyte level)
        {
            RunIndex = runIndex;
            Run = run;
            Level = level;
            NextRunIndex = -1;
        }

        public int RunIndex { get; }

        public sbyte Level { get; }

        public TextRun Run { get; }

        public int NextRunIndex { get; set; } // -1 if none
    }
}
