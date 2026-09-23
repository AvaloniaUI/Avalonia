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
    private string[]? _availableLanguages;

    public IReadOnlyList<CultureInfo> SupportedCultures
    {
        get
        {
            var available = GetAvailableLanguages();
            var cultures = new List<CultureInfo>(available.Length);

            foreach (var language in available)
            {
                try
                {
                    cultures.Add(CultureInfo.GetCultureInfo(language.Replace('_', '-')));
                }
                catch (CultureNotFoundException)
                {
                }
            }

            return cultures;
        }
    }

    public ISpellCheckContext? CreateContext(CultureInfo culture)
    {
        var language = ResolveLanguage(culture);
        return language is null ? null : new IOSContext(language);
    }

    private sealed class IOSContext : SpellCheckContextBase
    {
        private readonly string _language;
        private readonly UITextChecker _checker = new();

        public IOSContext(string language)
        {
            _language = language;
        }

        protected override ValueTask<IReadOnlyList<ISpellCheckResult>> CheckCoreAsync(
            ReadOnlyMemory<char> textMemory,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (textMemory.IsEmpty)
            {
                return new ValueTask<IReadOnlyList<ISpellCheckResult>>(Array.Empty<ISpellCheckResult>());
            }

            var text = textMemory.ToString();
            List<ISpellCheckResult>? results = null;
            var range = new NSRange(0, text.Length);
            nint offset = 0;

            while (offset < text.Length)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var misspelled = _checker.RangeOfMisspelledWordInString(text, range, offset, false, _language);

                if (misspelled.Location == NSRange.NotFound || misspelled.Length <= 0)
                    break;

                var start = checked((int)misspelled.Location);
                var length = checked((int)misspelled.Length);

                if (start < 0 || start + length > text.Length)
                    break;

                (results ??= new List<ISpellCheckResult>()).Add(
                    new IOSResult(this, start, length, text.Substring(start, length)));
                offset = start + length;
            }

            return new ValueTask<IReadOnlyList<ISpellCheckResult>>(
                results is null ? Array.Empty<ISpellCheckResult>() : results);
        }

        public ValueTask<IReadOnlyList<string>> SuggestCoreAsync(string word, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new ValueTask<IReadOnlyList<string>>(
                _checker.GuessesForWordRange(new NSRange(0, word.Length), word, _language) ?? Array.Empty<string>());
        }

        protected override void DisposeCore()
        {
            _checker.Dispose();
        }
    }

    private sealed class IOSResult : SpellCheckResultBase
    {
        private readonly IOSContext _context;
        private readonly string _word;

        public IOSResult(IOSContext context, int start, int length, string word)
            : base(context, start, length)
        {
            _context = context;
            _word = word;
        }

        protected override ValueTask<IReadOnlyList<string>> SuggestCoreAsync(CancellationToken cancellationToken) =>
            _context.SuggestCoreAsync(_word, cancellationToken);
    }

    // Match UIKit's language tags, falling back to another region of the same language.
    private string? ResolveLanguage(CultureInfo culture)
    {
        return GetSupportedLanguageTag(culture.Name, culture.TwoLetterISOLanguageName);
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
