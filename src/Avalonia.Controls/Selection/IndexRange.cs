// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;
using System.Collections.Generic;

#nullable enable

namespace Avalonia.Controls.Selection
{
    internal readonly struct IndexRange : IEquatable<IndexRange>
    {
        private static readonly IndexRange s_invalid = new IndexRange(int.MinValue, int.MinValue);

        public IndexRange(int index)
        {
            Begin = index;
            End = index;
        }

        public IndexRange(int begin, int end)
        {
            // Accept out of order begin/end pairs, just swap them.
            if (begin > end)
            {
                int temp = begin;
                begin = end;
                end = temp;
            }

            Begin = begin;
            End = end;
        }

        public int Begin { get; }
        public int End { get; }
        public int Count => (End - Begin) + 1;

        public bool Contains(int index) => index >= Begin && index <= End;

        public bool Split(int splitIndex, out IndexRange before, out IndexRange after)
        {
            bool afterIsValid;

            before = new IndexRange(Begin, splitIndex);

            if (splitIndex < End)
            {
                after = new IndexRange(splitIndex + 1, End);
                afterIsValid = true;
            }
            else
            {
                after = new IndexRange();
                afterIsValid = false;
            }

            return afterIsValid;
        }

        public bool Intersects(IndexRange other)
        {
            return (Begin <= other.End) && (End >= other.Begin);
        }

        public bool Adjacent(IndexRange other)
        {
            return Begin == other.End + 1 || End == other.Begin - 1;
        }

        public override bool Equals(object? obj)
        {
            return obj is IndexRange range && Equals(range);
        }

        public bool Equals(IndexRange other)
        {
            return Begin == other.Begin && End == other.End;
        }

        public override int GetHashCode()
        {
            var hashCode = 1903003160;
            hashCode = hashCode * -1521134295 + Begin.GetHashCode();
            hashCode = hashCode * -1521134295 + End.GetHashCode();
            return hashCode;
        }

        public override string ToString() => $"[{Begin}..{End}]";

        public static bool operator ==(IndexRange left, IndexRange right) => left.Equals(right);
        public static bool operator !=(IndexRange left, IndexRange right) => !(left == right);

        public static bool Contains(IReadOnlyList<IndexRange>? ranges, int index)
        {
            if (ranges is null || index < 0)
            {
                return false;
            }

            foreach (var range in ranges)
            {
                if (range.Contains(index))
                {
                    return true;
                }
            }

            return false;
        }

        public static int GetAt(IReadOnlyList<IndexRange> ranges, int index)
        {
            var currentIndex = 0;

            foreach (var range in ranges)
            {
                var currentCount = range.Count;

                if (index >= currentIndex && index < currentIndex + currentCount)
                {
                    return range.Begin + (index - currentIndex);
                }

                currentIndex += currentCount;
            }

            throw new IndexOutOfRangeException("The index was out of range.");
        }

        public static int Add(
            IList<IndexRange> ranges,
            IndexRange range,
            IList<IndexRange>? added = null)
        {
            var result = 0;

            for (var i = 0; i < ranges.Count && range != s_invalid; ++i)
            {
                var existing = ranges[i];

                if (range.Intersects(existing) || range.Adjacent(existing))
                {
                    if (range.Begin < existing.Begin)
                    {
                        var add = new IndexRange(range.Begin, existing.Begin - 1);
                        ranges[i] = new IndexRange(range.Begin, existing.End);
                        added?.Add(add);
                        result += add.Count;
                    }

                    range = range.End <= existing.End ?
                        s_invalid :
                        new IndexRange(existing.End + 1, range.End);
                }
                else if (range.End < existing.Begin)
                {
                    ranges.Insert(i, range);
                    added?.Add(range);
                    result += range.Count;
                    range = s_invalid;
                }
            }

            if (range != s_invalid)
            {
                ranges.Add(range);
                added?.Add(range);
                result += range.Count;
            }

            MergeRanges(ranges);
            return result;
        }

        public static int Add(
            IList<IndexRange> destination,
            IReadOnlyList<IndexRange> source,
            IList<IndexRange>? added = null)
        {
            var result = 0;

            foreach (var range in source)
            {
                result += Add(destination, range, added);
            }

            return result;
        }

        public static int Intersect(
            IList<IndexRange> ranges,
            IndexRange range,
            IList<IndexRange>? removed = null)
        {
            var result = 0;

            for (var i = 0; i < ranges.Count && range != s_invalid; ++i)
            {
                var existing = ranges[i];

                if (existing.End < range.Begin || existing.Begin > range.End)
                {
                    removed?.Add(existing);
                    ranges.RemoveAt(i--);
                    result += existing.Count;
                }
                else
                {
                    if (existing.Begin < range.Begin)
                    {
                        var except = new IndexRange(existing.Begin, range.Begin - 1);
                        removed?.Add(except);
                        ranges[i] = existing = new IndexRange(range.Begin, existing.End);
                        result += except.Count;
                    }

                    if (existing.End > range.End)
                    {
                        var except = new IndexRange(range.End + 1, existing.End);
                        removed?.Add(except);
                        ranges[i] = new IndexRange(existing.Begin, range.End);
                        result += except.Count;
                    }
                }
            }

            MergeRanges(ranges);

            if (removed is object)
            {
                MergeRanges(removed);
            }

            return result;
        }

        public static int Remove(
            IList<IndexRange>? ranges,
            IndexRange range,
            IList<IndexRange>? removed = null)
        {
            if (ranges is null)
            {
                return 0;
            }

            var result = 0;

            for (var i = 0; i < ranges.Count; ++i)
            {
                var existing = ranges[i];

                if (range.Intersects(existing))
                {
                    if (range.Begin <= existing.Begin && range.End >= existing.End)
                    {
                        ranges.RemoveAt(i--);
                        removed?.Add(existing);
                        result += existing.Count;
                    }
                    else if (range.Begin > existing.Begin && range.End >= existing.End)
                    {
                        ranges[i] = new IndexRange(existing.Begin, range.Begin - 1);
                        removed?.Add(new IndexRange(range.Begin, existing.End));
                        result += existing.End - (range.Begin - 1);
                    }
                    else if (range.Begin > existing.Begin && range.End < existing.End)
                    {
                        ranges[i] = new IndexRange(existing.Begin, range.Begin - 1);
                        ranges.Insert(++i, new IndexRange(range.End + 1, existing.End));
                        removed?.Add(range);
                        result += range.Count;
                    }
                    else if (range.End <= existing.End)
                    {
                        var remove = new IndexRange(existing.Begin, range.End);
                        ranges[i] = new IndexRange(range.End + 1, existing.End);
                        removed?.Add(remove);
                        result += remove.Count;
                    }
                }
            }

            return result;
        }

        public static int Remove(
            IList<IndexRange> destination,
            IReadOnlyList<IndexRange> source,
            IList<IndexRange>? added = null)
        {
            var result = 0;

            foreach (var range in source)
            {
                result += Remove(destination, range, added);
            }

            return result;
        }

        public static IEnumerable<int> EnumerateIndices(IEnumerable<IndexRange> ranges)
        {
            foreach (var range in ranges)
            {
                for (var i = range.Begin; i <= range.End; ++i)
                {
                    yield return i;
                }
            }
        }

        public static int GetCount(IEnumerable<IndexRange> ranges)
        {
            var result = 0;

            foreach (var range in ranges)
            {
                result += (range.End - range.Begin) + 1;
            }

            return result;
        }

        private static void MergeRanges(IList<IndexRange> ranges)
        {
            for (var i = ranges.Count - 2; i >= 0; --i)
            {
                var r = ranges[i];
                var r1 = ranges[i + 1];

                if (r.Intersects(r1) || r.End == r1.Begin - 1)
                {
                    ranges[i] = new IndexRange(r.Begin, r1.End);
                    ranges.RemoveAt(i + 1);
                }
            }
        }
    }
}
