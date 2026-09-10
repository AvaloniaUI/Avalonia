using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Input.TextInput;
using CoreFoundation;
using Foundation;
using ObjCRuntime;
using UIKit;

namespace Avalonia.iOS;

internal sealed class IOSSpellCheckProvider : ISpellCheckProvider
{
    private UITextChecker? _checker;
    private string[]? _availableLanguages;
    private string? _defaultLanguage;
    private bool _defaultLanguageResolved;

    public bool IsLanguageSupported(CultureInfo? culture)
    {
        return ResolveLanguage(culture) is not null;
    }

    public ValueTask<IReadOnlyList<SpellCheckResult>> CheckAsync(
        ReadOnlyMemory<char> textMemory,
        CultureInfo? culture,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var language = ResolveLanguage(culture);

        if (textMemory.IsEmpty || language is null || GetChecker() is not { } checker)
        {
            return new ValueTask<IReadOnlyList<SpellCheckResult>>(Array.Empty<SpellCheckResult>());
        }

        var text = textMemory.ToString();
        List<SpellCheckResult>? results = null;
        var range = new NSRange(0, text.Length);
        nint offset = 0;

        while (offset < text.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var misspelled = checker.RangeOfMisspelledWordInString(text, range, offset, false, language);

            if (misspelled.Location == NSRange.NotFound || misspelled.Length <= 0)
            {
                break;
            }

            var start = checked((int)misspelled.Location);
            var length = checked((int)misspelled.Length);

            if (start < 0 || start + length > text.Length)
            {
                break;
            }

            (results ??= new List<SpellCheckResult>()).Add(new SpellCheckResult(start, length));
            offset = start + length;
        }

        return new ValueTask<IReadOnlyList<SpellCheckResult>>(
            results is null ? Array.Empty<SpellCheckResult>() : results);
    }

    public ValueTask<IReadOnlyList<string>> SuggestAsync(
        string word,
        CultureInfo? culture,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var language = ResolveLanguage(culture);

        if (string.IsNullOrWhiteSpace(word) || language is null || GetChecker() is not { } checker)
        {
            return new ValueTask<IReadOnlyList<string>>(Array.Empty<string>());
        }

        return new ValueTask<IReadOnlyList<string>>(
            checker.GuessesForWordRange(new NSRange(0, word.Length), word, language) ?? Array.Empty<string>());
    }

    private UITextChecker? GetChecker()
    {
        if (_checker is not null)
        {
            return _checker;
        }

        // Let initialization errors reach the manager so they can be logged and retried.
        return _checker = new UITextChecker();
    }

    // Match UIKit's language tags, falling back to another region of the same language.
    private string? ResolveLanguage(CultureInfo? culture)
    {
        if (culture is null)
        {
            if (!_defaultLanguageResolved)
            {
                _defaultLanguage = ResolveDefaultLanguage();
                // Cache only successful resolution so native failures can be retried.
                _defaultLanguageResolved = true;
            }

            return _defaultLanguage;
        }

        return GetSupportedLanguageTag(culture.Name, culture.TwoLetterISOLanguageName);
    }

    private string? ResolveDefaultLanguage()
    {
        foreach (var preferred in NSLocale.PreferredLanguages)
        {
            if (string.IsNullOrEmpty(preferred))
            {
                continue;
            }

            if (GetSupportedLanguageTag(preferred, null) is { } supported)
            {
                return supported;
            }
        }

        var current = CultureInfo.CurrentCulture;

        return string.IsNullOrEmpty(current.Name)
            ? null
            : GetSupportedLanguageTag(current.Name, current.TwoLetterISOLanguageName);
    }

    private string? GetSupportedLanguageTag(string languageTag, string? neutralLanguageTag)
    {
        var available = GetAvailableLanguages();

        if (available.Length == 0 || string.IsNullOrEmpty(languageTag))
        {
            return null;
        }

        var normalized = languageTag.Replace('-', '_');

        foreach (var language in available)
        {
            if (string.Equals(language, normalized, StringComparison.OrdinalIgnoreCase))
            {
                return language;
            }
        }

        // Fall back to the same language in another region.
        if (string.IsNullOrEmpty(neutralLanguageTag))
        {
            var separator = normalized.IndexOf('_');
            neutralLanguageTag = separator > 0 ? normalized.Substring(0, separator) : normalized;
        }

        foreach (var language in available)
        {
            if (string.Equals(language, neutralLanguageTag, StringComparison.OrdinalIgnoreCase))
            {
                return language;
            }
        }

        var prefix = neutralLanguageTag + "_";

        foreach (var language in available)
        {
            if (language.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return language;
            }
        }

        return null;
    }

    private string[] GetAvailableLanguages()
    {
        if (_availableLanguages is not null)
        {
            return _availableLanguages;
        }

        // The binding has the wrong return type; call the native NSArray<NSString*> API directly.
        var handle = IntPtr_objc_msgSend(
            Class.GetHandle("UITextChecker"),
            Selector.GetHandle("availableLanguages"));

        _availableLanguages = handle == IntPtr.Zero
            ? Array.Empty<string>()
            : RemoveNullValues(CFArray.StringArrayFromHandle(handle));

        return _availableLanguages;
    }

    private static string[] RemoveNullValues(string?[]? values)
    {
        if (values is null || values.Length == 0)
        {
            return Array.Empty<string>();
        }

        List<string>? result = null;

        for (var i = 0; i < values.Length; i++)
        {
            if (values[i] is { } value)
            {
                (result ??= new List<string>(values.Length)).Add(value);
            }
        }

        return result?.ToArray() ?? Array.Empty<string>();
    }

    [DllImport(Constants.ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    private static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, IntPtr selector);
}
