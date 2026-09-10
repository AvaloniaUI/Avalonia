using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Input.TextInput;

namespace Avalonia.Native;

internal sealed class MacOSSpellCheckProvider : ISpellCheckProvider
{
    private static readonly IntPtr s_nSSpellCheckerClass = ObjectiveC.GetClass("NSSpellChecker");
    private static readonly IntPtr s_availableLanguagesSelector = ObjectiveC.GetSelector("availableLanguages");
    private static readonly IntPtr s_sharedSpellCheckerSelector = ObjectiveC.GetSelector("sharedSpellChecker");
    private static readonly IntPtr s_checkSpellingSelector = ObjectiveC.GetSelector("checkSpellingOfString:startingAt:language:wrap:inSpellDocumentWithTag:wordCount:");
    private static readonly IntPtr s_guessesSelector = ObjectiveC.GetSelector("guessesForWordRange:inString:language:inSpellDocumentWithTag:");
    private static readonly IntPtr s_countSelector = ObjectiveC.GetSelector("count");
    private static readonly IntPtr s_objectAtIndexSelector = ObjectiveC.GetSelector("objectAtIndex:");
    private static readonly IntPtr s_languageSelector = ObjectiveC.GetSelector("language");
    private static readonly IntPtr s_nSLocaleClass = ObjectiveC.GetClass("NSLocale");
    private static readonly IntPtr s_preferredLanguagesSelector = ObjectiveC.GetSelector("preferredLanguages");

    private readonly string[] _availableLanguages;
    private readonly IntPtr _spellChecker;
    private string? _defaultLanguage;
    private bool _defaultLanguageResolved;

    // Share the native checker and language list across windows.
    public static MacOSSpellCheckProvider Instance { get; } = new();

    private MacOSSpellCheckProvider()
    {
        if (!OperatingSystem.IsMacOS() || s_nSSpellCheckerClass == IntPtr.Zero)
        {
            _availableLanguages = [];
            return;
        }

        _spellChecker = ObjectiveC.SendIntPtr(s_nSSpellCheckerClass, s_sharedSpellCheckerSelector);
        _availableLanguages = CopyStringArray(ObjectiveC.SendIntPtr(_spellChecker, s_availableLanguagesSelector));
    }

    public bool IsLanguageSupported(CultureInfo? culture)
    {
        if (_spellChecker == IntPtr.Zero)
        {
            return false;
        }

        return TryGetLanguage(culture, out _);
    }

    public ValueTask<IReadOnlyList<SpellCheckResult>> CheckAsync(
        ReadOnlyMemory<char> textMemory,
        CultureInfo? culture,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var text = textMemory.Span;

        if (_spellChecker == IntPtr.Zero ||
            text.IsEmpty ||
            !TryGetLanguage(culture, out var language))
        {
            return new ValueTask<IReadOnlyList<SpellCheckResult>>(Array.Empty<SpellCheckResult>());
        }

        var results = new List<SpellCheckResult>();
        var textHandle = CoreFoundationString.Create(text);
        var languageHandle = CoreFoundationString.Create(language!);

        try
        {
            nint offset = 0;

            while (offset < text.Length)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var misspelled = ObjectiveC.CheckSpelling(
                    _spellChecker,
                    s_checkSpellingSelector,
                    textHandle,
                    offset,
                    languageHandle,
                    false,
                    0,
                    out _);

                if (misspelled.Location == ObjectiveC.NSNotFound || misspelled.Length <= 0)
                {
                    break;
                }

                var start = checked((int)misspelled.Location);
                var length = checked((int)misspelled.Length);

                results.Add(new SpellCheckResult(start, length));
                offset = misspelled.Location + misspelled.Length;
            }
        }
        finally
        {
            CoreFoundationString.Release(languageHandle);
            CoreFoundationString.Release(textHandle);
        }

        return new ValueTask<IReadOnlyList<SpellCheckResult>>(results);
    }

    public ValueTask<IReadOnlyList<string>> SuggestAsync(
        string word,
        CultureInfo? culture,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_spellChecker == IntPtr.Zero ||
            string.IsNullOrWhiteSpace(word) ||
            !TryGetLanguage(culture, out var language))
        {
            return new ValueTask<IReadOnlyList<string>>(Array.Empty<string>());
        }

        var wordHandle = CoreFoundationString.Create(word);
        var languageHandle = CoreFoundationString.Create(language!);

        try
        {
            var guesses = ObjectiveC.GetGuesses(
                _spellChecker,
                s_guessesSelector,
                new ObjectiveC.NSRange(0, word.Length),
                wordHandle,
                languageHandle,
                0);

            return new ValueTask<IReadOnlyList<string>>(CopyStringArray(guesses));
        }
        finally
        {
            CoreFoundationString.Release(languageHandle);
            CoreFoundationString.Release(wordHandle);
        }
    }

    // NSSpellChecker needs an explicit language. For null culture, use its current language,
    // then user preferences and the process culture.
    private bool TryGetLanguage(CultureInfo? culture, out string? language)
    {
        if (culture is not null)
        {
            language = GetSupportedLanguageTag(culture);
            return language is not null;
        }

        if (!_defaultLanguageResolved)
        {
            _defaultLanguage = ResolveDefaultLanguage();
            // Cache only successful language resolution so failures can be retried.
            _defaultLanguageResolved = true;
        }

        language = _defaultLanguage;
        return language is not null;
    }

    private string? ResolveDefaultLanguage()
    {
        if (_spellChecker == IntPtr.Zero)
        {
            return null;
        }

        // The spelling language is empty when set to automatic.
        var current = CoreFoundationString.ToString(ObjectiveC.SendIntPtr(_spellChecker, s_languageSelector));

        if (!string.IsNullOrEmpty(current) && GetSupportedLanguageTag(current, null) is { } fromChecker)
        {
            return fromChecker;
        }

        if (s_nSLocaleClass != IntPtr.Zero)
        {
            foreach (var preferred in CopyStringArray(ObjectiveC.SendIntPtr(s_nSLocaleClass, s_preferredLanguagesSelector)))
            {
                if (GetSupportedLanguageTag(preferred, null) is { } fromLocale)
                {
                    return fromLocale;
                }
            }
        }

        foreach (var fallback in new[] { CultureInfo.CurrentUICulture, CultureInfo.CurrentCulture })
        {
            if (!string.IsNullOrEmpty(fallback.Name) && GetSupportedLanguageTag(fallback) is { } fromCulture)
            {
                return fromCulture;
            }
        }

        return null;
    }

    private string? GetSupportedLanguageTag(CultureInfo culture)
    {
        return GetSupportedLanguageTag(culture.Name, culture.TwoLetterISOLanguageName);
    }

    private string? GetSupportedLanguageTag(string languageTag, string? neutralLanguageTag)
    {
        foreach (var candidate in GetLanguageTagCandidates(languageTag, neutralLanguageTag))
        {
            foreach (var availableLanguage in _availableLanguages)
            {
                if (string.Equals(candidate, availableLanguage, StringComparison.OrdinalIgnoreCase))
                {
                    return availableLanguage;
                }
            }
        }

        return null;
    }

    private static IEnumerable<string> GetLanguageTagCandidates(string languageTag, string? neutralLanguageTag)
    {
        if (string.IsNullOrEmpty(neutralLanguageTag))
        {
            var separator = languageTag.IndexOfAny(new[] { '-', '_' });
            neutralLanguageTag = separator > 0 ? languageTag.Substring(0, separator) : null;
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

        if (!string.IsNullOrEmpty(neutralLanguageTag) &&
            !string.Equals(neutralLanguageTag, languageTag, StringComparison.OrdinalIgnoreCase))
        {
            yield return neutralLanguageTag;
        }
    }

    private static string[] CopyStringArray(IntPtr array)
    {
        if (array == IntPtr.Zero)
        {
            return [];
        }

        var count = ObjectiveC.SendNUInt(array, s_countSelector);

        if (count == 0)
        {
            return [];
        }

        var stringCount = checked((int)count);
        var strings = new string[stringCount];

        for (var i = 0; i < stringCount; i++)
        {
            strings[i] = CoreFoundationString.ToString(
                ObjectiveC.SendIntPtr(array, s_objectAtIndexSelector, (nuint)i));
        }

        return strings;
    }

    private static class ObjectiveC
    {
        public static readonly nint NSNotFound = nint.MaxValue;

        private const string AppKit = "/System/Library/Frameworks/AppKit.framework/AppKit";
        private const string LibObjC = "/usr/lib/libobjc.A.dylib";
        private static bool s_appKitLoaded;

        [DllImport(LibObjC, EntryPoint = "objc_getClass")]
        private static extern unsafe IntPtr GetClassCore(byte* name);

        [DllImport(LibObjC, EntryPoint = "sel_registerName")]
        private static extern unsafe IntPtr GetSelectorCore(byte* name);

        [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
        private static extern IntPtr SendIntPtrCore(IntPtr receiver, IntPtr selector);

        [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
        private static extern IntPtr SendIntPtrCore(IntPtr receiver, IntPtr selector, nuint arg1);

        [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
        private static extern nuint SendNUIntCore(IntPtr receiver, IntPtr selector);

        [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
        private static extern unsafe NSRange CheckSpellingCore(
            IntPtr receiver,
            IntPtr selector,
            IntPtr text,
            nint startingOffset,
            IntPtr language,
            byte wrap,
            nint spellDocumentTag,
            nint* wordCount);

        [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
        private static extern IntPtr GetGuessesCore(
            IntPtr receiver,
            IntPtr selector,
            NSRange range,
            IntPtr text,
            IntPtr language,
            nint spellDocumentTag);

        public static IntPtr GetClass(string name)
        {
            try
            {
                EnsureAppKitLoaded();

                var bytes = Encoding.UTF8.GetBytes(name + '\0');

                unsafe
                {
                    fixed (byte* bytesPtr = bytes)
                    {
                        return GetClassCore(bytesPtr);
                    }
                }
            }
            catch (DllNotFoundException)
            {
                return IntPtr.Zero;
            }
            catch (EntryPointNotFoundException)
            {
                return IntPtr.Zero;
            }
        }

        public static IntPtr GetSelector(string name)
        {
            if (!OperatingSystem.IsMacOS())
            {
                return IntPtr.Zero;
            }

            var bytes = Encoding.UTF8.GetBytes(name + '\0');

            unsafe
            {
                fixed (byte* bytesPtr = bytes)
                {
                    return GetSelectorCore(bytesPtr);
                }
            }
        }

        public static IntPtr SendIntPtr(IntPtr receiver, IntPtr selector) =>
            receiver == IntPtr.Zero || selector == IntPtr.Zero
                ? IntPtr.Zero
                : SendIntPtrCore(receiver, selector);

        public static IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, nuint arg1) =>
            receiver == IntPtr.Zero || selector == IntPtr.Zero
                ? IntPtr.Zero
                : SendIntPtrCore(receiver, selector, arg1);

        public static nuint SendNUInt(IntPtr receiver, IntPtr selector) =>
            receiver == IntPtr.Zero || selector == IntPtr.Zero
                ? 0
                : SendNUIntCore(receiver, selector);

        public static NSRange CheckSpelling(
            IntPtr receiver,
            IntPtr selector,
            IntPtr text,
            nint startingOffset,
            IntPtr language,
            bool wrap,
            nint spellDocumentTag,
            out nint wordCount)
        {
            wordCount = 0;

            if (receiver == IntPtr.Zero || selector == IntPtr.Zero || text == IntPtr.Zero || language == IntPtr.Zero)
            {
                return new NSRange(NSNotFound, 0);
            }

            unsafe
            {
                nint nativeWordCount = 0;
                var result = CheckSpellingCore(
                    receiver,
                    selector,
                    text,
                    startingOffset,
                    language,
                    (byte)(wrap ? 1 : 0),
                    spellDocumentTag,
                    &nativeWordCount);

                wordCount = nativeWordCount;
                return result;
            }
        }

        public static IntPtr GetGuesses(
            IntPtr receiver,
            IntPtr selector,
            NSRange range,
            IntPtr text,
            IntPtr language,
            nint spellDocumentTag)
        {
            return receiver == IntPtr.Zero || selector == IntPtr.Zero || text == IntPtr.Zero || language == IntPtr.Zero
                ? IntPtr.Zero
                : GetGuessesCore(receiver, selector, range, text, language, spellDocumentTag);
        }

        private static void EnsureAppKitLoaded()
        {
            if (!s_appKitLoaded && OperatingSystem.IsMacOS())
            {
                s_appKitLoaded = NativeLibrary.TryLoad(AppKit, out _);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public readonly struct NSRange
        {
            public readonly nint Location;
            public readonly nint Length;

            public NSRange(nint location, nint length)
            {
                Location = location;
                Length = length;
            }
        }

    }

    private static class CoreFoundationString
    {
        private const string CoreFoundationLib = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
        private const uint UTF8Encoding = 0x08000100;

        [DllImport(CoreFoundationLib, EntryPoint = "CFStringCreateWithCharacters")]
        private static extern unsafe IntPtr CreateCore(
            IntPtr allocator,
            char* chars,
            nint numChars);

        [DllImport(CoreFoundationLib, EntryPoint = "CFStringGetLength")]
        private static extern nint GetLength(IntPtr value);

        [DllImport(CoreFoundationLib, EntryPoint = "CFStringGetMaximumSizeForEncoding")]
        private static extern nint GetMaximumSizeForEncoding(nint length, uint encoding);

        [DllImport(CoreFoundationLib, EntryPoint = "CFStringGetCString")]
        private static extern unsafe byte GetCString(IntPtr value, byte* buffer, nint bufferSize, uint encoding);

        [DllImport(CoreFoundationLib, EntryPoint = "CFRelease")]
        private static extern void ReleaseCore(IntPtr value);

        public static unsafe IntPtr Create(ReadOnlySpan<char> value)
        {
            fixed (char* chars = value)
            {
                return CreateCore(IntPtr.Zero, chars, value.Length);
            }
        }

        public static void Release(IntPtr value)
        {
            if (value != IntPtr.Zero)
            {
                ReleaseCore(value);
            }
        }

        public static string ToString(IntPtr value)
        {
            if (value == IntPtr.Zero)
            {
                return string.Empty;
            }

            var length = GetLength(value);
            var maximumByteCount = checked((int)GetMaximumSizeForEncoding(length, UTF8Encoding)) + 1;
            var buffer = new byte[maximumByteCount];

            unsafe
            {
                fixed (byte* bufferPtr = buffer)
                {
                    if (GetCString(value, bufferPtr, buffer.Length, UTF8Encoding) == 0)
                    {
                        return string.Empty;
                    }
                }
            }

            var byteCount = Array.IndexOf(buffer, (byte)0);

            return Encoding.UTF8.GetString(buffer, 0, byteCount < 0 ? buffer.Length : byteCount);
        }
    }
}
