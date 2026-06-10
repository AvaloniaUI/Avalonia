using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Input.TextInput;
using Foundation;
using UIKit;

namespace Avalonia.iOS;

internal sealed class IOSSpellCheckProvider : ISpellCheckProvider
{
    private readonly string _availableLanguages = UITextChecker.AvailableLangauges ?? string.Empty;
    private readonly UITextChecker _checker = new();

    public bool IsLanguageSupported(CultureInfo? culture)
    {
        return GetSupportedLanguageTag(culture) is not null;
    }

    public ValueTask<IReadOnlyList<SpellCheckResult>> CheckAsync(
        string text,
        CultureInfo? culture,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var language = GetSupportedLanguageTag(culture);

        if (string.IsNullOrEmpty(text) || language is null)
        {
            return new ValueTask<IReadOnlyList<SpellCheckResult>>(Array.Empty<SpellCheckResult>());
        }

        var results = new List<SpellCheckResult>();
        var range = new NSRange(0, text.Length);
        var offset = IntPtr.Zero;

        while (offset.ToInt64() < text.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var misspelled = _checker.RangeOfMisspelledWordInString(text, range, offset, false, language);

            if (misspelled.Location == NSRange.NotFound || misspelled.Length == 0)
            {
                break;
            }

            var start = checked((int)misspelled.Location);
            var length = checked((int)misspelled.Length);
            var word = start >= 0 && length > 0 && start + length <= text.Length
                ? text.Substring(start, length)
                : null;

            results.Add(new SpellCheckResult(start, length, word));
            offset = (IntPtr)(start + length);
        }

        return new ValueTask<IReadOnlyList<SpellCheckResult>>(results);
    }

    public ValueTask<IReadOnlyList<string>> SuggestAsync(
        string word,
        CultureInfo? culture,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var language = GetSupportedLanguageTag(culture);

        if (string.IsNullOrWhiteSpace(word) || language is null)
        {
            return new ValueTask<IReadOnlyList<string>>(Array.Empty<string>());
        }

        return new ValueTask<IReadOnlyList<string>>(
            _checker.GuessesForWordRange(new NSRange(0, word.Length), word, language) ??
            Array.Empty<string>());
    }

    private string? GetSupportedLanguageTag(CultureInfo? culture)
    {
        foreach (var candidate in GetLanguageTagCandidates(culture))
        {
            var availableLanguage = GetAvailableLanguage(candidate);

            if (availableLanguage is not null)
            {
                return availableLanguage;
            }
        }

        return null;
    }

    private string? GetAvailableLanguage(string candidate)
    {
        if (_availableLanguages.Length == 0)
        {
            return null;
        }

        var start = -1;

        for (var i = 0; i <= _availableLanguages.Length; i++)
        {
            var isSeparator = i == _availableLanguages.Length || IsLanguageSeparator(_availableLanguages[i]);

            if (!isSeparator && start < 0)
            {
                start = i;
            }
            else if (isSeparator && start >= 0)
            {
                var length = i - start;

                if (Matches(candidate, _availableLanguages, start, length))
                {
                    return _availableLanguages.Substring(start, length);
                }

                start = -1;
            }
        }

        return null;
    }

    private static bool Matches(string expected, string actual, int actualStart, int actualLength)
    {
        if (expected.Length == actualLength &&
            string.Compare(expected, 0, actual, actualStart, actualLength, StringComparison.OrdinalIgnoreCase) == 0)
        {
            return true;
        }

        var underscoreExpected = expected.Replace('-', '_');

        if (underscoreExpected.Length == actualLength &&
            string.Compare(underscoreExpected, 0, actual, actualStart, actualLength, StringComparison.OrdinalIgnoreCase) == 0)
        {
            return true;
        }

        var hyphenExpected = expected.Replace('_', '-');

        return hyphenExpected.Length == actualLength &&
            string.Compare(hyphenExpected, 0, actual, actualStart, actualLength, StringComparison.OrdinalIgnoreCase) == 0;
    }

    private static bool IsLanguageSeparator(char value)
    {
        return char.IsWhiteSpace(value) ||
            value is ',' or ';' or '(' or ')' or '[' or ']' or '"' or '\'';
    }

    private static IEnumerable<string> GetLanguageTagCandidates(CultureInfo? culture)
    {
        var languageTag = culture?.Name;

        if (string.IsNullOrEmpty(languageTag))
        {
            languageTag = CultureInfo.CurrentCulture.Name;
        }

        if (!string.IsNullOrEmpty(languageTag))
        {
            yield return languageTag;

            var underscoreLanguageTag = languageTag.Replace('-', '_');

            if (underscoreLanguageTag != languageTag)
            {
                yield return underscoreLanguageTag;
            }
        }

        var neutralLanguageTag = culture?.TwoLetterISOLanguageName;

        if (string.IsNullOrEmpty(neutralLanguageTag) && !string.IsNullOrEmpty(languageTag))
        {
            try
            {
                neutralLanguageTag = new CultureInfo(languageTag).TwoLetterISOLanguageName;
            }
            catch (CultureNotFoundException)
            {
            }
        }

        if (!string.IsNullOrEmpty(neutralLanguageTag))
        {
            yield return neutralLanguageTag;
        }
    }
}
