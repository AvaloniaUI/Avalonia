using System;
using System.Collections.Generic;
using Avalonia.Input.TextInput;

namespace Avalonia.Controls;

internal sealed class SpellCheckResultCache
{
    private IReadOnlyList<SpellCheckResult> _results = Array.Empty<SpellCheckResult>();
    private IReadOnlyList<SpellCheckRange> _checkedRanges = Array.Empty<SpellCheckRange>();
    private string? _checkedText;

    public IReadOnlyList<SpellCheckResult> Results => _results;

    public void Clear()
    {
        _results = Array.Empty<SpellCheckResult>();
        _checkedRanges = Array.Empty<SpellCheckRange>();
        _checkedText = null;
    }

    public void Set(
        string text,
        List<SpellCheckRange> ranges,
        IReadOnlyList<SpellCheckResult> results,
        bool merge)
    {
        if (merge && string.Equals(_checkedText, text, StringComparison.Ordinal))
        {
            _results = MergeResults(_results, ranges, results);
            _checkedRanges = MergeCheckedRanges(_checkedRanges, ranges);
        }
        else
        {
            _results = results;
            _checkedRanges = ranges.Count == 0 ? Array.Empty<SpellCheckRange>() : ranges.ToArray();
        }

        _checkedText = text;
    }

    public bool AreRangesChecked(string? text, List<SpellCheckRange> ranges)
    {
        if (ranges.Count == 0 ||
            _checkedRanges.Count == 0 ||
            !string.Equals(_checkedText, text, StringComparison.Ordinal))
        {
            return false;
        }

        for (var i = 0; i < ranges.Count; i++)
        {
            if (!IsRangeChecked(ranges[i]))
            {
                return false;
            }
        }

        return true;
    }

    public bool TryGetMisspelledWord(
        string text,
        int caretIndex,
        int selectionStart,
        int selectionEnd,
        out SpellCheckResult result)
    {
        result = default;

        if (_results.Count == 0 || string.IsNullOrEmpty(text))
        {
            return false;
        }

        var selectedStart = Math.Min(selectionStart, selectionEnd);
        var selectedEnd = Math.Max(selectionStart, selectionEnd);
        var hasSelection = selectedStart != selectedEnd;

        foreach (var candidate in _results)
        {
            var candidateStart = candidate.Start;
            var candidateEnd = candidate.Start + candidate.Length;
            var isMatch = hasSelection
                ? candidateStart < selectedEnd && candidateEnd > selectedStart
                : caretIndex >= candidateStart && caretIndex <= candidateEnd;

            if (!isMatch)
            {
                continue;
            }

            result = candidate.Word is not null
                ? candidate
                : candidate with
                {
                    Word = candidateStart >= 0 && candidateEnd <= text.Length
                        ? text.Substring(candidateStart, candidate.Length)
                        : null
                };

            return true;
        }

        return false;
    }

    private bool IsRangeChecked(SpellCheckRange range)
    {
        for (var i = 0; i < _checkedRanges.Count; i++)
        {
            var checkedRange = _checkedRanges[i];

            if (range.Start >= checkedRange.Start && range.End <= checkedRange.End)
            {
                return true;
            }
        }

        return false;
    }

    private static IReadOnlyList<SpellCheckResult> MergeResults(
        IReadOnlyList<SpellCheckResult> existing,
        List<SpellCheckRange> ranges,
        IReadOnlyList<SpellCheckResult> results)
    {
        if (existing.Count == 0)
        {
            if (results.Count == 0)
            {
                return Array.Empty<SpellCheckResult>();
            }

            var sorted = new List<SpellCheckResult>(results);
            SortResults(sorted);
            return sorted;
        }

        var merged = new List<SpellCheckResult>(existing.Count + results.Count);

        for (var i = 0; i < existing.Count; i++)
        {
            if (!IntersectsAnyRange(existing[i], ranges))
            {
                merged.Add(existing[i]);
            }
        }

        for (var i = 0; i < results.Count; i++)
        {
            merged.Add(results[i]);
        }

        if (merged.Count == 0)
        {
            return Array.Empty<SpellCheckResult>();
        }

        SortResults(merged);
        return merged;
    }

    private static IReadOnlyList<SpellCheckRange> MergeCheckedRanges(
        IReadOnlyList<SpellCheckRange> existing,
        List<SpellCheckRange> ranges)
    {
        if (existing.Count == 0)
        {
            return ranges.Count == 0 ? Array.Empty<SpellCheckRange>() : ranges.ToArray();
        }

        if (ranges.Count == 0)
        {
            return existing;
        }

        var merged = new List<SpellCheckRange>(existing.Count + ranges.Count);

        for (var i = 0; i < existing.Count; i++)
        {
            merged.Add(existing[i]);
        }

        for (var i = 0; i < ranges.Count; i++)
        {
            merged.Add(ranges[i]);
        }

        merged.Sort(static (x, y) =>
        {
            var start = x.Start.CompareTo(y.Start);
            return start != 0 ? start : x.End.CompareTo(y.End);
        });

        var writeIndex = 0;

        for (var readIndex = 1; readIndex < merged.Count; readIndex++)
        {
            var current = merged[readIndex];
            var last = merged[writeIndex];

            if (current.Start <= last.End)
            {
                if (current.End > last.End)
                {
                    merged[writeIndex] = new SpellCheckRange(last.Start, current.End);
                }
            }
            else
            {
                writeIndex++;
                merged[writeIndex] = current;
            }
        }

        if (writeIndex + 1 < merged.Count)
        {
            merged.RemoveRange(writeIndex + 1, merged.Count - writeIndex - 1);
        }

        return merged;
    }

    private static bool IntersectsAnyRange(SpellCheckResult result, List<SpellCheckRange> ranges)
    {
        var resultEnd = result.Start + result.Length;

        for (var i = 0; i < ranges.Count; i++)
        {
            var range = ranges[i];

            if (resultEnd > range.Start && result.Start < range.End)
            {
                return true;
            }
        }

        return false;
    }

    private static void SortResults(List<SpellCheckResult> results)
    {
        results.Sort(static (x, y) =>
        {
            var start = x.Start.CompareTo(y.Start);
            return start != 0 ? start : x.Length.CompareTo(y.Length);
        });
    }
}
