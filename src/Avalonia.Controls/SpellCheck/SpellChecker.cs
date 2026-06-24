using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Input.TextInput;

namespace Avalonia.Controls;

internal static class SpellChecker
{
    private const int MaxSuggestionCount = 8;

    public static async ValueTask<IReadOnlyList<SpellCheckResult>> CheckRangesAsync(
        string text,
        List<SpellCheckRange> ranges,
        ISpellCheckProvider provider,
        CultureInfo culture,
        CancellationToken cancellationToken)
    {
        List<SpellCheckResult>? normalized = null;

        for (var i = 0; i < ranges.Count; i++)
        {
            var range = ranges[i];
            var length = range.End - range.Start;

            if (length <= 0 || IsWhiteSpace(text, range.Start, length))
            {
                continue;
            }

            var rangeText = text.Substring(range.Start, length);
            var results = await provider.CheckAsync(rangeText, culture, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            if (results.Count == 0)
            {
                continue;
            }

            AddNormalizedResults(results, range, length, normalized ??= new List<SpellCheckResult>(results.Count));
        }

        if (normalized is null || normalized.Count == 0)
        {
            return Array.Empty<SpellCheckResult>();
        }

        SortResults(normalized);
        return normalized;
    }

    public static IReadOnlyList<string> NormalizeSuggestions(string word, IReadOnlyList<string> suggestions)
    {
        if (suggestions.Count == 0)
        {
            return Array.Empty<string>();
        }

        var normalized = new List<string>(Math.Min(suggestions.Count, MaxSuggestionCount));
        var seen = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase)
        {
            word
        };

        foreach (var suggestion in suggestions)
        {
            var value = suggestion.Trim();

            if (value.Length == 0 || !seen.Add(value))
            {
                continue;
            }

            normalized.Add(value);

            if (normalized.Count == MaxSuggestionCount)
            {
                break;
            }
        }

        return normalized.Count == 0 ? Array.Empty<string>() : normalized;
    }

    private static void AddNormalizedResults(
        IReadOnlyList<SpellCheckResult> results,
        SpellCheckRange range,
        int textLength,
        List<SpellCheckResult> normalized)
    {
        foreach (var result in results)
        {
            if (result.Start < 0 || result.Length <= 0 || result.Start >= textLength)
            {
                continue;
            }

            var length = Math.Min(result.Length, textLength - result.Start);

            // Avoid underlining a partial word produced by a horizontally clipped visible range.
            if ((range.StartIsInsideWord && result.Start == 0) ||
                (range.EndIsInsideWord && result.Start + length >= textLength))
            {
                continue;
            }

            normalized.Add(result with { Start = range.Start + result.Start, Length = length });
        }
    }

    private static bool IsWhiteSpace(string text, int start, int length)
    {
        var end = Math.Min(text.Length, start + length);

        for (var i = start; i < end; i++)
        {
            if (!char.IsWhiteSpace(text[i]))
            {
                return false;
            }
        }

        return true;
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
