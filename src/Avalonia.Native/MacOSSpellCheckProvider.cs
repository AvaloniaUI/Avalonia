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

    private readonly string[] _availableLanguages;
    private readonly IntPtr _spellChecker;

    public MacOSSpellCheckProvider()
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
        return GetSupportedLanguageTag(culture) is not null;
    }

    public ValueTask<IReadOnlyList<SpellCheckResult>> CheckAsync(
        ReadOnlySpan<char> text,
        CultureInfo? culture,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_spellChecker == IntPtr.Zero ||
            text.IsEmpty ||
            GetSupportedLanguageTag(culture) is not { } language)
        {
            return new ValueTask<IReadOnlyList<SpellCheckResult>>(Array.Empty<SpellCheckResult>());
        }

        var results = new List<SpellCheckResult>();
        var textHandle = CoreFoundationString.Create(text);
        var languageHandle = CoreFoundationString.Create(language);

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
                var word = start >= 0 && length > 0 && start + length <= text.Length
                    ? text.Slice(start, length).ToString()
                    : null;

                results.Add(new SpellCheckResult(start, length, word));
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
            GetSupportedLanguageTag(culture) is not { } language)
        {
            return new ValueTask<IReadOnlyList<string>>(Array.Empty<string>());
        }

        var wordHandle = CoreFoundationString.Create(word);
        var languageHandle = CoreFoundationString.Create(language);

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

    private string? GetSupportedLanguageTag(CultureInfo? culture)
    {
        foreach (var candidate in GetLanguageTagCandidates(culture))
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
