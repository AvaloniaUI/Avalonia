using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Input.TextInput;
using Avalonia.Win32.Interop;
using Avalonia.Win32.Win32Com;
using MicroCom.Runtime;

namespace Avalonia.Win32.Input;

/// <summary>
/// Spell checking through the Windows 8+ Spell Checking API (<c>ISpellCheckerFactory</c>).
/// </summary>
/// <remarks>
/// COM spell checkers are bound to their creating UI thread. Reuse one provider per UI thread.
/// </remarks>
internal sealed unsafe class Win32SpellCheckProvider : ISpellCheckProvider, ISpellCheckProviderWithLanguageIdentity
{
    private const int StackallocCharLimit = 256;
    private const int S_FALSE = 1;

    // CORRECTIVE_ACTION from spellcheck.h
    private const uint CorrectiveActionDelete = 3;

    private static readonly Guid s_spellCheckerFactoryClsid = new("7AB36653-1796-484B-BDFA-E74F1DB7C1DC");

    [ThreadStatic]
    private static Win32SpellCheckProvider? t_instance;

    private readonly Dictionary<string, ISpellChecker> _spellCheckers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, bool> _supportedLanguageTags = new(StringComparer.OrdinalIgnoreCase);
    private ISpellCheckerFactory? _factory;
    private bool _factoryFailed;

    private Win32SpellCheckProvider()
    {
    }

    public static Win32SpellCheckProvider GetForCurrentThread() => t_instance ??= new Win32SpellCheckProvider();

    public bool IsLanguageSupported(CultureInfo? culture)
    {
        return GetFactory() is { } factory && GetSupportedLanguageTag(factory, culture) is not null;
    }

    public ValueTask<IReadOnlyList<SpellCheckResult>> CheckAsync(
        ReadOnlyMemory<char> textMemory,
        CultureInfo? culture,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var text = textMemory.Span;

        if (text.IsEmpty || GetSpellChecker(culture) is not { } spellChecker)
        {
            return new ValueTask<IReadOnlyList<SpellCheckResult>>(Array.Empty<SpellCheckResult>());
        }

        List<SpellCheckResult>? results = null;
        char[]? rented = null;

        try
        {
            Span<char> textBuffer = text.Length < StackallocCharLimit
                ? stackalloc char[text.Length + 1]
                : rented = ArrayPool<char>.Shared.Rent(text.Length + 1);

            text.CopyTo(textBuffer);
            textBuffer[text.Length] = '\0';

            fixed (char* textPtr = textBuffer)
            {
                using var errors = spellChecker.Check(textPtr);

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    void* errorPtr = null;
                    var hr = unchecked((int)errors.Next(&errorPtr));

                    if (hr < 0)
                    {
                        throw new COMException("IEnumSpellingError::Next failed", hr);
                    }

                    if (hr == S_FALSE || errorPtr is null)
                    {
                        break;
                    }

                    using var error = MicroComRuntime.CreateProxyFor<ISpellingError>(errorPtr, true);
                    var start = checked((int)error.StartIndex);
                    var length = checked((int)error.Length);

                    if (start < 0 || length <= 0 || start + length > text.Length)
                    {
                        continue;
                    }

                    // Ignore repeated-word deletions: they have no spelling suggestions.
                    if (error.CorrectiveAction == CorrectiveActionDelete)
                    {
                        continue;
                    }

                    (results ??= new List<SpellCheckResult>()).Add(new SpellCheckResult(start, length));
                }
            }
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<char>.Shared.Return(rented);
            }
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

        if (string.IsNullOrWhiteSpace(word) || GetSpellChecker(culture) is not { } spellChecker)
        {
            return new ValueTask<IReadOnlyList<string>>(Array.Empty<string>());
        }

        List<string>? results = null;

        fixed (char* wordPtr = word)
        {
            void* suggestionsPtr = null;
            var hr = unchecked((int)spellChecker.Suggest(wordPtr, &suggestionsPtr));

            if (hr < 0)
            {
                throw new COMException("ISpellChecker::Suggest failed", hr);
            }

            using var suggestions = MicroComRuntime.CreateProxyOrNullFor<IEnumString>(suggestionsPtr, true);

            // S_FALSE: the word is spelled correctly (the enumerator just echoes it back).
            if (hr != S_FALSE && suggestions is not null)
            {
                ReadStrings(suggestions, ref results, cancellationToken);
            }
        }

        return new ValueTask<IReadOnlyList<string>>(results is null ? Array.Empty<string>() : results);
    }

    private static void ReadStrings(IEnumString strings, ref List<string>? results, CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            char* value = null;
            uint fetched = 0;
            var hr = unchecked((int)strings.Next(1, &value, &fetched));

            if (hr < 0)
            {
                throw new COMException("IEnumString::Next failed", hr);
            }

            if (hr == S_FALSE || fetched == 0 || value is null)
            {
                return;
            }

            string? result;

            try
            {
                result = Marshal.PtrToStringUni((IntPtr)value);
            }
            finally
            {
                Marshal.FreeCoTaskMem((IntPtr)value);
            }

            if (!string.IsNullOrEmpty(result))
            {
                (results ??= new List<string>()).Add(result);
            }
        }
    }

    private ISpellChecker? GetSpellChecker(CultureInfo? culture)
    {
        if (GetFactory() is not { } factory ||
            GetSupportedLanguageTag(factory, culture) is not { } languageTag)
        {
            return null;
        }

        if (_spellCheckers.TryGetValue(languageTag, out var spellChecker))
        {
            return spellChecker;
        }

        fixed (char* languageTagPtr = languageTag)
        {
            spellChecker = factory.CreateSpellChecker(languageTagPtr);
        }

        _spellCheckers[languageTag] = spellChecker;
        return spellChecker;
    }

    string? ISpellCheckProviderWithLanguageIdentity.GetLanguageIdentity(CultureInfo? culture)
    {
        return GetFactory() is { } factory
            ? GetSupportedLanguageTag(factory, culture)
            : null;
    }

    private ISpellCheckerFactory? GetFactory()
    {
        if (_factory is not null)
        {
            return _factory;
        }

        if (_factoryFailed ||
            Win32Platform.WindowsVersion < PlatformConstants.Windows8 ||
            OleContext.Current is null)
        {
            return null;
        }

        try
        {
            var iid = MicroComRuntime.GetGuidFor(typeof(ISpellCheckerFactory));
            return _factory = UnmanagedMethods.CreateInstance<ISpellCheckerFactory>(in s_spellCheckerFactoryClsid, in iid);
        }
        catch (COMException)
        {
            // No spell checking platform component (N editions, server SKUs, ...).
            _factoryFailed = true;
            return null;
        }
    }

    private string? GetSupportedLanguageTag(ISpellCheckerFactory factory, CultureInfo? culture)
    {
        foreach (var languageTag in GetLanguageTagCandidates(culture))
        {
            if (IsSupported(factory, languageTag))
            {
                return languageTag;
            }
        }

        return null;
    }

    private bool IsSupported(ISpellCheckerFactory factory, string languageTag)
    {
        // Cache language support to avoid repeated COM calls.
        if (_supportedLanguageTags.TryGetValue(languageTag, out var supported))
        {
            return supported;
        }

        fixed (char* languageTagPtr = languageTag)
        {
            supported = factory.IsSupported(languageTagPtr) != 0;
        }

        _supportedLanguageTags[languageTag] = supported;
        return supported;
    }

    // Use the keyboard input language, then the UI language, when no culture is specified.
    private static IEnumerable<string> GetLanguageTagCandidates(CultureInfo? culture)
    {
        if (culture is not null)
        {
            foreach (var tag in GetLanguageTagCandidates(culture.Name, culture.TwoLetterISOLanguageName))
            {
                yield return tag;
            }

            yield break;
        }

        var inputLanguage = GetKeyboardLayoutCulture();

        if (inputLanguage is not null)
        {
            foreach (var tag in GetLanguageTagCandidates(inputLanguage.Name, inputLanguage.TwoLetterISOLanguageName))
            {
                yield return tag;
            }
        }

        foreach (var fallback in new[] { CultureInfo.CurrentUICulture, CultureInfo.CurrentCulture })
        {
            if (!string.IsNullOrEmpty(fallback.Name))
            {
                foreach (var tag in GetLanguageTagCandidates(fallback.Name, fallback.TwoLetterISOLanguageName))
                {
                    yield return tag;
                }
            }
        }
    }

    private static IEnumerable<string> GetLanguageTagCandidates(string languageTag, string? neutralLanguageTag)
    {
        if (!string.IsNullOrEmpty(languageTag))
        {
            yield return languageTag;
        }

        if (!string.IsNullOrEmpty(neutralLanguageTag) &&
            !string.Equals(neutralLanguageTag, languageTag, StringComparison.OrdinalIgnoreCase))
        {
            yield return neutralLanguageTag;
        }
    }

    private static CultureInfo? GetKeyboardLayoutCulture()
    {
        try
        {
            // The low word of the keyboard layout handle is the input language identifier.
            var langId = (int)((long)UnmanagedMethods.GetKeyboardLayout(0) & 0xFFFF);
            return langId == 0 ? null : CultureInfo.GetCultureInfo(langId);
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }
}
