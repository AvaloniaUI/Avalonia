using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Input.TextInput;
using Avalonia.Threading;

namespace Avalonia.Controls;

internal static class SpellChecker
{
    private const int MaxSuggestionCount = 8;
    // Native checks may block the UI thread. Limit chunk size and yield between calls.
    internal const int MaxProviderCheckLength = 2048;

    public static async ValueTask<IReadOnlyList<SpellCheckResult>> CheckRangesAsync(
        string text,
        List<SpellCheckRange> ranges,
        ISpellCheckProvider provider,
        CultureInfo? culture,
        CancellationToken cancellationToken)
    {
        List<SpellCheckResult>? normalized = null;
        var boundaries = new SpellCheckTokenization.WordBoundaryFinder(text);

        for (var i = 0; i < ranges.Count; i++)
        {
            var range = ranges[i];
            var chunkStart = range.Start;

            while (chunkStart < range.End)
            {
                var maximum = Math.Min(range.End, chunkStart + MaxProviderCheckLength);
                // Split natural text at Unicode word boundaries, but keep addresses and identifiers intact.
                var checkStart = Math.Min(boundaries.End(chunkStart), maximum);
                var checkEnd = Math.Max(boundaries.Start(maximum), chunkStart);
                var chunkEnd = maximum == range.End || checkEnd <= chunkStart ? maximum : checkEnd;
                var length = checkEnd - checkStart;

                if (length > 0 && !IsWhiteSpace(text.AsSpan(checkStart, length)))
                {
                    var results = await provider.CheckAsync(
                        text.AsMemory(checkStart, length), culture, cancellationToken);

                    cancellationToken.ThrowIfCancellationRequested();

                    if (results.Count > 0)
                    {
                        AddNormalizedResults(
                            results, checkStart, length, normalized ??= new List<SpellCheckResult>(results.Count));
                    }
                }

                chunkStart = chunkEnd;

                if (chunkStart < range.End || i + 1 < ranges.Count)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Dispatcher.Yield(DispatcherPriority.Background);
                }
            }
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
        int rangeStart,
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

            normalized.Add(result with { Start = rangeStart + result.Start, Length = length });
        }
    }

    private static bool IsWhiteSpace(ReadOnlySpan<char> text)
    {
        for (var i = 0; i < text.Length; i++)
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
