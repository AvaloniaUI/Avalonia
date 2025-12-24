using System;
using System.Collections.Generic;

namespace Avalonia.Utilities;

/// <summary>
/// Helpers for splitting strings.
/// </summary>
internal static class StringSplitter
{
    private const char DefaultOpeningParenthesis = '(';
    private const char DefaultClosingParenthesis = ')';

    /// <summary>
    /// Splits the provided string by the specified separators, but ignores separators that
    /// appear inside matching bracket pairs (<paramref name="openingBracket"/> / <paramref name="closingBracket"/>).
    /// </summary>
    /// <param name="s">The input string to split. If <c>null</c>, an empty array is returned.</param>
    /// <param name="separator">The separator character to split on.</param>
    /// <param name="openingBracket">The character that opens a bracketed section. <c>(</c> by default.</param>
    /// <param name="closingBracket">The character that closes a bracketed section. <c>)</c> by default.</param>
    /// <param name="options">Options for trimming entries and removing empty entries.</param>
    /// <returns>An array of split segments. Returns an empty array if the input is null or only whitespace.</returns>
    public static string[] SplitRespectingBrackets(string? s, char separator,
        char openingBracket = DefaultOpeningParenthesis, char closingBracket = DefaultClosingParenthesis,
        StringSplitOptions options = StringSplitOptions.None) =>
        SplitRespectingBrackets(s, [separator], openingBracket, closingBracket, options);

    /// <summary>
    /// Splits the provided string by the specified separator, but ignores separators that
    /// appear inside matching bracket pairs (<paramref name="openingBracket"/> / <paramref name="closingBracket"/>).
    /// </summary>
    /// <param name="s">The input string to split. If <c>null</c>, an empty array is returned.</param>
    /// <param name="separators">The separator characters to split on.</param>
    /// <param name="openingBracket">The character that opens a bracketed section. <c>(</c> by default.</param>
    /// <param name="closingBracket">The character that closes a bracketed section. <c>)</c> by default.</param>
    /// <param name="options">Options for trimming entries and removing empty entries.</param>
    /// <returns>An array of split segments. Returns an empty array if the input is null or only whitespace.</returns>
    public static string[] SplitRespectingBrackets(string? s, ReadOnlySpan<char> separators,
        char openingBracket = DefaultOpeningParenthesis, char closingBracket = DefaultClosingParenthesis,
        StringSplitOptions options = StringSplitOptions.None)
    {
        if (openingBracket == closingBracket)
            throw new ArgumentException($"Opening bracket and closing bracket cannot be the same character '{openingBracket}'.", nameof(closingBracket));

        if (s is null)
            return [];

        var span = s.AsSpan();

        var ranges = new List<(int start, int length)>();
        int depth = 0;
        int segStart = 0;

        bool removeEmptyEntries = options.HasFlag(StringSplitOptions.RemoveEmptyEntries);
        bool trimEntries = options.HasFlag(StringSplitOptions.TrimEntries);

        for (int i = 0; i < span.Length; i++)
        {
            char ch = span[i];
            if (ch == openingBracket)
                depth++;
            else if (ch == closingBracket)
            {
                if (depth <= 0)
                    throw new FormatException($"Unmatched closing bracket '{closingBracket}' at position {i}.");
                depth--;
            }
            else if (separators.Contains(ch))
            {
                if (depth != 0)
                    continue;
                ProcessSegment(segStart, i - 1);
                segStart = i + 1;
            }
        }

        if (depth != 0)
            throw new FormatException($"Unmatched opening bracket '{openingBracket}' in input string.");
        // last segment
        ProcessSegment(segStart, span.Length - 1);

        if (ranges.Count == 0)
            return [];

        var result = new string[ranges.Count];
        for (int i = 0; i < ranges.Count; i++)
        {
            var r = ranges[i];
#if NET6_0_OR_GREATER
            result[i] = new string(span.Slice(r.start, r.length));
#else
            result[i] = span.Slice(r.start, r.length).ToString();
#endif
        }

        return result;

        void ProcessSegment(int start, int end)
        {
            if (trimEntries)
            {
                while (start <= end && char.IsWhiteSpace(s[start]))
                    start++;
                while (end >= start && char.IsWhiteSpace(s[end]))
                    end--;
            }

            int length = end - start + 1;
            if (length > 0 || !removeEmptyEntries)
                ranges.Add((start, length));
        }
    }
}
