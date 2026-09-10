using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Input.TextInput;

namespace Avalonia.Controls;

// Keeps checked ranges and misspellings across edits and scrolling.
internal sealed class SpellCheckResultCache
{
    private IReadOnlyList<SpellCheckResult> _results = Array.Empty<SpellCheckResult>();
    private IReadOnlyList<SpellCheckRange> _checkedRanges = Array.Empty<SpellCheckRange>();
    private string? _checkedText;

    public IReadOnlyList<SpellCheckResult> Results => _results;

    // Suppressed while the caret stays in this word.
    public SpellCheckRange? LastEditedWord { get; private set; }

    public void ClearLastEditedWord() => LastEditedWord = null;

    // Keep LastEditedWord until the next edit or caret move.
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
        // Clipped tokens were not checked and must not overwrite cached results.
        var fullyCheckedRanges = GetFullyCheckedRanges(text, ranges);

        if (merge && string.Equals(_checkedText, text, StringComparison.Ordinal))
        {
            _results = MergeResults(_results, fullyCheckedRanges, results);
            _checkedRanges = MergeCheckedRanges(_checkedRanges, fullyCheckedRanges);
        }
        else
        {
            _results = results;
            _checkedRanges = fullyCheckedRanges.Count == 0
                ? Array.Empty<SpellCheckRange>()
                : fullyCheckedRanges.ToArray();
        }

        _checkedText = text;
    }

    private static List<SpellCheckRange> GetFullyCheckedRanges(string text, List<SpellCheckRange> ranges)
    {
        List<SpellCheckRange>? result = null;
        SpellCheckTokenization.WordBoundaryFinder? boundaries = null;

        for (var i = 0; i < ranges.Count; i++)
        {
            var range = ranges[i];

            if (!range.StartIsInsideWord && !range.EndIsInsideWord)
            {
                result?.Add(range);
                continue;
            }

            if (result is null)
            {
                result = new List<SpellCheckRange>(ranges.Count);

                for (var j = 0; j < i; j++)
                {
                    result.Add(ranges[j]);
                }
            }

            boundaries ??= new SpellCheckTokenization.WordBoundaryFinder(text);
            var start = range.StartIsInsideWord ? boundaries.End(range.Start) : range.Start;
            var end = range.EndIsInsideWord ? boundaries.Start(range.End) : range.End;

            if (end > start)
            {
                result.Add(new SpellCheckRange(start, end));
            }
        }

        return result ?? ranges;
    }

    // Shift unaffected ranges and invalidate edited tokens; false means the cache is stale.
    // Track the edited word only for user input, not programmatic changes or undo/redo.
    public bool TryApplyEdit(string? oldText, string newText, bool trackEditedWord)
    {
        LastEditedWord = null;

        if (oldText is null || string.Equals(oldText, newText, StringComparison.Ordinal))
        {
            return _checkedText is not null && string.Equals(_checkedText, newText, StringComparison.Ordinal);
        }

        var prefix = CommonPrefixLength(oldText, newText);

        // Keep edit boundaries outside surrogate pairs.
        if (SplitsScalarAt(oldText, prefix) || SplitsScalarAt(newText, prefix))
        {
            prefix--;
        }

        var maxSuffix = Math.Min(oldText.Length, newText.Length) - prefix;
        var suffix = CommonSuffixLength(oldText, newText, maxSuffix);

        if (SplitsScalarAt(oldText, oldText.Length - suffix) ||
            SplitsScalarAt(newText, newText.Length - suffix))
        {
            suffix--;
        }

        var removed = oldText.Length - prefix - suffix;
        var added = newText.Length - prefix - suffix;
        var delta = added - removed;

        // Editing an address suffix can change whether the whole token is checkable.
        var charBeforeIsToken = prefix > 0 && !SpellCheckTokenization.IsBoundary(newText[prefix - 1]);
        var charAfterIsToken = prefix + added < newText.Length && !SpellCheckTokenization.IsBoundary(newText[prefix + added]);
        var firstChangedIsToken = (removed > 0 && !SpellCheckTokenization.IsBoundary(oldText[prefix])) ||
            (added > 0 && !SpellCheckTokenization.IsBoundary(newText[prefix]));
        var lastChangedIsToken = (removed > 0 && !SpellCheckTokenization.IsBoundary(oldText[prefix + removed - 1])) ||
            (added > 0 && !SpellCheckTokenization.IsBoundary(newText[prefix + added - 1]));
        var extendBack = charBeforeIsToken && (firstChangedIsToken || charAfterIsToken);
        var extendForward = charAfterIsToken && (lastChangedIsToken || charBeforeIsToken);

        var invalidStart = extendBack ? SpellCheckTokenization.Start(newText, prefix) : prefix;
        var oldInvalidEnd = extendForward ? SpellCheckTokenization.End(oldText, prefix + removed) : prefix + removed;
        var newInvalidEnd = extendForward ? SpellCheckTokenization.End(newText, prefix + added) : prefix + added;

        if (trackEditedWord)
        {
            LastEditedWord = GetEditedWord(
                newText,
                prefix + added,
                invalidStart,
                newInvalidEnd,
                allowFollowingWord: added == 0 && removed > 0);
        }

        if (_checkedText is null || !string.Equals(_checkedText, oldText, StringComparison.Ordinal))
        {
            // The edited word is known, but the cached results are stale.
            return false;
        }

        _results = RebaseResults(_results, invalidStart, oldInvalidEnd, delta, newText.Length);
        _checkedRanges = RebaseCheckedRanges(_checkedRanges, invalidStart, oldInvalidEnd, delta, newText.Length);
        _checkedText = newText;

        return true;
    }

    public bool AreRangesChecked(string? text, List<SpellCheckRange> ranges)
    {
        if (ranges.Count == 0 ||
            _checkedRanges.Count == 0 ||
            !string.Equals(_checkedText, text, StringComparison.Ordinal))
        {
            return false;
        }

        var checkedIndex = 0;

        for (var i = 0; i < ranges.Count; i++)
        {
            var range = ranges[i];

            while (checkedIndex < _checkedRanges.Count &&
                   _checkedRanges[checkedIndex].End <= range.Start)
            {
                checkedIndex++;
            }

            if (checkedIndex >= _checkedRanges.Count ||
                range.Start < _checkedRanges[checkedIndex].Start ||
                range.End > _checkedRanges[checkedIndex].End)
            {
                return false;
            }
        }

        return true;
    }

    // Return unchecked ranges without splitting words or addresses.
    public List<SpellCheckRange> GetUncheckedRanges(string text, List<SpellCheckRange> ranges)
    {
        var result = new List<SpellCheckRange>();

        if (!string.Equals(_checkedText, text, StringComparison.Ordinal) || _checkedRanges.Count == 0)
        {
            result.AddRange(ranges);
            return result;
        }

        var boundaries = new SpellCheckTokenization.WordBoundaryFinder(text);

        var checkedIndex = 0;

        for (var i = 0; i < ranges.Count; i++)
        {
            var range = ranges[i];
            var cursor = range.Start;

            // _checkedRanges is sorted and non-overlapping (see MergeCheckedRanges).
            while (checkedIndex < _checkedRanges.Count &&
                   _checkedRanges[checkedIndex].End <= cursor)
            {
                checkedIndex++;
            }

            var currentCheckedIndex = checkedIndex;

            while (currentCheckedIndex < _checkedRanges.Count && cursor < range.End)
            {
                var checkedRange = _checkedRanges[currentCheckedIndex];

                if (checkedRange.Start >= range.End)
                {
                    break;
                }

                if (checkedRange.Start > cursor)
                {
                    AddUncheckedRange(result, boundaries, cursor, checkedRange.Start, range);
                }

                cursor = Math.Max(cursor, checkedRange.End);

                // The next viewport fragment may overlap this same checked range.
                if (checkedRange.End < range.End)
                {
                    currentCheckedIndex++;
                }
                else
                {
                    break;
                }
            }

            checkedIndex = currentCheckedIndex;

            if (cursor < range.End)
            {
                AddUncheckedRange(result, boundaries, cursor, range.End, range);
            }
        }

        return result;
    }

    private static void AddUncheckedRange(List<SpellCheckRange> result, SpellCheckTokenization.WordBoundaryFinder boundaries, int start, int end, SpellCheckRange within)
    {
        // Expand to word boundaries without losing the viewport's clipped-token flags.
        start = Math.Max(within.Start, boundaries.Start(start));
        end = Math.Min(within.End, boundaries.End(end));

        if (end <= start)
        {
            return;
        }

        if (result.Count > 0)
        {
            var last = result[result.Count - 1];

            if (start <= last.End)
            {
                if (end > last.End)
                {
                    result[result.Count - 1] = new SpellCheckRange(last.Start, end, last.StartIsInsideWord, within.EndIsInsideWord && end == within.End);
                }

                return;
            }
        }

        result.Add(new SpellCheckRange(
            start,
            end,
            within.StartIsInsideWord && start == within.Start,
            within.EndIsInsideWord && end == within.End));
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

    private static int CommonPrefixLength(string a, string b)
    {
        var max = Math.Min(a.Length, b.Length);
        var i = 0;

        while (i < max && a[i] == b[i])
        {
            i++;
        }

        return i;
    }

    private static int CommonSuffixLength(string a, string b, int max)
    {
        var i = 0;

        while (i < max && a[a.Length - 1 - i] == b[b.Length - 1 - i])
        {
            i++;
        }

        return i;
    }

    private static bool SplitsScalarAt(string text, int index)
    {
        return index > 0 &&
            index < text.Length &&
            char.IsHighSurrogate(text[index - 1]) &&
            char.IsLowSurrogate(text[index]);
    }

    private static SpellCheckRange? GetEditedWord(
        string text,
        int editEnd,
        int invalidStart,
        int invalidEnd,
        bool allowFollowingWord)
    {
        editEnd = Math.Clamp(editEnd, 0, text.Length);
        var wordBefore = IsWordCharBefore(text, editEnd);

        // Prefer the word before the caret; the following word may be untouched.
        if (!wordBefore && !(allowFollowingWord && editEnd < invalidEnd && IsWordCharAt(text, editEnd)))
        {
            return null;
        }

        var start = WordStart(text, editEnd);
        var end = WordEnd(text, editEnd);

        // Suppress the following word only if the edit touched it.
        return end > start && start < invalidEnd && end > invalidStart
            ? new SpellCheckRange(start, end)
            : null;
    }

    internal static int WordStart(string text, int index)
    {
        index = Math.Min(index, text.Length);

        while (index > 0)
        {
            var previous = PreviousScalarStart(text, index);

            if (!IsWordCharAt(text, previous))
            {
                break;
            }

            index = previous;
        }

        return index;
    }

    internal static int WordEnd(string text, int index)
    {
        index = Math.Max(0, Math.Min(index, text.Length));

        while (index < text.Length && IsWordCharAt(text, index))
        {
            index = NextScalarEnd(text, index);
        }

        return index;
    }

    internal static bool IsWordChar(char c)
    {
        return char.IsLetterOrDigit(c) || c is '\'' or '’' or '-' || char.GetUnicodeCategory(c) is
            System.Globalization.UnicodeCategory.NonSpacingMark or
            System.Globalization.UnicodeCategory.SpacingCombiningMark or
            System.Globalization.UnicodeCategory.EnclosingMark;
    }

    internal static bool IsWordCharAt(string text, int index)
    {
        if ((uint)index >= (uint)text.Length)
        {
            return false;
        }

        var first = text[index];

        if (char.IsHighSurrogate(first) &&
            index + 1 < text.Length &&
            Rune.TryCreate(first, text[index + 1], out var rune))
        {
            return IsWordRune(rune);
        }

        return !char.IsSurrogate(first) && IsWordChar(first);
    }

    internal static bool IsWordCharBefore(string text, int index)
    {
        return index > 0 && IsWordCharAt(text, PreviousScalarStart(text, index));
    }

    internal static int PreviousScalarStart(string text, int index)
    {
        index = Math.Clamp(index, 0, text.Length);

        if (index == 0)
        {
            return 0;
        }

        var result = index - 1;

        if (result > 0 && char.IsLowSurrogate(text[result]) && char.IsHighSurrogate(text[result - 1]))
        {
            result--;
        }

        return result;
    }

    internal static int NextScalarEnd(string text, int index)
    {
        index = Math.Clamp(index, 0, text.Length);

        if (index == text.Length)
        {
            return index;
        }

        return char.IsHighSurrogate(text[index]) &&
            index + 1 < text.Length &&
            char.IsLowSurrogate(text[index + 1])
                ? index + 2
                : index + 1;
    }

    private static bool IsWordRune(Rune rune)
    {
        return Rune.IsLetterOrDigit(rune) || rune.Value is '\'' or '’' or '-' || Rune.GetUnicodeCategory(rune) is
            System.Globalization.UnicodeCategory.NonSpacingMark or
            System.Globalization.UnicodeCategory.SpacingCombiningMark or
            System.Globalization.UnicodeCategory.EnclosingMark;
    }

    private static IReadOnlyList<SpellCheckResult> RebaseResults(
        IReadOnlyList<SpellCheckResult> results,
        int invalidStart,
        int oldInvalidEnd,
        int delta,
        int newTextLength)
    {
        if (results.Count == 0)
        {
            return results;
        }

        List<SpellCheckResult>? rebased = null;

        for (var i = 0; i < results.Count; i++)
        {
            var result = results[i];
            var end = result.Start + result.Length;

            if (end <= invalidStart)
            {
                (rebased ??= new List<SpellCheckResult>(results.Count)).Add(result);
            }
            else if (result.Start >= oldInvalidEnd)
            {
                var start = result.Start + delta;

                if (start >= 0 && start + result.Length <= newTextLength)
                {
                    (rebased ??= new List<SpellCheckResult>(results.Count)).Add(result with { Start = start });
                }
            }
            // Drop results touched by the edit so they can be checked again.
        }

        return rebased is null ? Array.Empty<SpellCheckResult>() : rebased;
    }

    private static IReadOnlyList<SpellCheckRange> RebaseCheckedRanges(
        IReadOnlyList<SpellCheckRange> ranges,
        int invalidStart,
        int oldInvalidEnd,
        int delta,
        int newTextLength)
    {
        if (ranges.Count == 0)
        {
            return ranges;
        }

        var rebased = new List<SpellCheckRange>(ranges.Count + 1);

        for (var i = 0; i < ranges.Count; i++)
        {
            var range = ranges[i];

            if (range.Start < invalidStart)
            {
                AddOrMergeCheckedRange(
                    rebased,
                    new SpellCheckRange(range.Start, Math.Min(range.End, invalidStart), range.StartIsInsideWord, false));
            }

            if (range.End > oldInvalidEnd)
            {
                var start = Math.Max(range.Start, oldInvalidEnd) + delta;
                var end = Math.Min(range.End + delta, newTextLength);

                if (end > start)
                {
                    AddOrMergeCheckedRange(
                        rebased,
                        new SpellCheckRange(start, end, false, range.EndIsInsideWord));
                }
            }
        }

        return rebased.Count == 0 ? Array.Empty<SpellCheckRange>() : rebased;
    }

    private static void AddOrMergeCheckedRange(List<SpellCheckRange> ranges, SpellCheckRange range)
    {
        if (range.End <= range.Start)
        {
            return;
        }

        if (ranges.Count == 0 || range.Start > ranges[ranges.Count - 1].End)
        {
            ranges.Add(range);
            return;
        }

        var last = ranges[ranges.Count - 1];

        if (range.End > last.End)
        {
            ranges[ranges.Count - 1] = new SpellCheckRange(
                last.Start,
                range.End,
                last.StartIsInsideWord,
                range.EndIsInsideWord);
        }
    }

    private static IReadOnlyList<SpellCheckResult> MergeResults(
        IReadOnlyList<SpellCheckResult> existing,
        List<SpellCheckRange> ranges,
        IReadOnlyList<SpellCheckResult> results)
    {
        var newResults = CopySortedResults(results);

        if (existing.Count == 0)
        {
            return newResults;
        }

        var merged = new List<SpellCheckResult>(existing.Count + results.Count);
        var rangeIndex = 0;
        var newResultIndex = 0;

        for (var i = 0; i < existing.Count; i++)
        {
            var result = existing[i];

            // Sorted ranges let us discard replaced results in one pass.
            while (rangeIndex < ranges.Count && ranges[rangeIndex].End <= result.Start)
            {
                rangeIndex++;
            }

            if (rangeIndex >= ranges.Count ||
                result.Start + result.Length <= ranges[rangeIndex].Start)
            {
                while (newResultIndex < newResults.Count &&
                       CompareResults(newResults[newResultIndex], result) <= 0)
                {
                    merged.Add(newResults[newResultIndex++]);
                }

                merged.Add(result);
            }
        }

        while (newResultIndex < newResults.Count)
        {
            merged.Add(newResults[newResultIndex++]);
        }

        return merged.Count == 0 ? Array.Empty<SpellCheckResult>() : merged;
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
        var existingIndex = 0;
        var rangeIndex = 0;

        // Merge the sorted inputs without sorting the full cache again.
        while (existingIndex < existing.Count || rangeIndex < ranges.Count)
        {
            SpellCheckRange next;

            if (rangeIndex >= ranges.Count ||
                (existingIndex < existing.Count && existing[existingIndex].Start <= ranges[rangeIndex].Start))
            {
                next = existing[existingIndex++];
            }
            else
            {
                next = ranges[rangeIndex++];
            }

            AddMergedRange(merged, next);
        }

        return merged;
    }

    private static void AddMergedRange(List<SpellCheckRange> ranges, SpellCheckRange range)
    {
        if (ranges.Count == 0 || range.Start > ranges[ranges.Count - 1].End)
        {
            ranges.Add(range);
            return;
        }

        var last = ranges[ranges.Count - 1];

        if (range.End > last.End)
        {
            ranges[ranges.Count - 1] = new SpellCheckRange(last.Start, range.End);
        }
    }

    private static void SortResults(List<SpellCheckResult> results)
    {
        results.Sort(static (x, y) => CompareResults(x, y));
    }

    private static IReadOnlyList<SpellCheckResult> CopySortedResults(IReadOnlyList<SpellCheckResult> results)
    {
        if (results.Count == 0)
        {
            return Array.Empty<SpellCheckResult>();
        }

        var copy = new List<SpellCheckResult>(results);

        for (var i = 1; i < copy.Count; i++)
        {
            if (CompareResults(copy[i - 1], copy[i]) > 0)
            {
                SortResults(copy);
                break;
            }
        }

        return copy;
    }

    private static int CompareResults(SpellCheckResult x, SpellCheckResult y)
    {
        var start = x.Start.CompareTo(y.Start);
        return start != 0 ? start : x.Length.CompareTo(y.Length);
    }
}
