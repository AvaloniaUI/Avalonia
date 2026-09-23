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
internal sealed unsafe class Win32SpellCheckProvider : ISpellCheckProvider
{
    private const int StackallocCharLimit = 256;
    private const int S_FALSE = 1;

    // CORRECTIVE_ACTION from spellcheck.h.
    private const uint CorrectiveActionReplace = 2;
    private const uint CorrectiveActionDelete = 3;

    private static readonly Guid s_spellCheckerFactoryClsid = new("7AB36653-1796-484B-BDFA-E74F1DB7C1DC");

    [ThreadStatic]
    private static Win32SpellCheckProvider? t_instance;

    private readonly Dictionary<string, bool> _supportedLanguageTags = new(StringComparer.OrdinalIgnoreCase);

    // Creating a checker is expensive, so every context of a language shares one for the provider's lifetime.
    private readonly Dictionary<string, ISpellChecker> _spellCheckers = new(StringComparer.OrdinalIgnoreCase);
    private ISpellCheckerFactory? _factory;
    private bool _factoryFailed;

    private Win32SpellCheckProvider()
    {
    }

    public static Win32SpellCheckProvider GetForCurrentThread() => t_instance ??= new Win32SpellCheckProvider();

    public IReadOnlyList<CultureInfo> SupportedCultures
    {
        get
        {
            if (GetFactory() is not { } factory)
                return Array.Empty<CultureInfo>();

            List<string>? languageTags = null;
            using var supportedLanguages = factory.SupportedLanguages;
            ReadStrings(supportedLanguages, ref languageTags, CancellationToken.None);

            if (languageTags is null)
                return Array.Empty<CultureInfo>();

            var cultures = new List<CultureInfo>(languageTags.Count);

            foreach (var languageTag in languageTags)
            {
                try
                {
                    cultures.Add(CultureInfo.GetCultureInfo(languageTag));
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
        if (GetFactory() is not { } factory ||
            GetSupportedLanguageTag(factory, culture) is not { } languageTag)
        {
            return null;
        }

        if (!_spellCheckers.TryGetValue(languageTag, out var spellChecker))
        {
            fixed (char* languageTagPtr = languageTag)
            {
                spellChecker = factory.CreateSpellChecker(languageTagPtr);
            }

            _spellCheckers[languageTag] = spellChecker;
        }

        return new Win32Context(spellChecker);
    }

    private sealed class Win32Context : SpellCheckContextBase
    {
        private readonly ISpellChecker _spellChecker;

        public Win32Context(ISpellChecker spellChecker)
        {
            _spellChecker = spellChecker;
        }

        protected override ValueTask<IReadOnlyList<ISpellCheckResult>> CheckCoreAsync(
            ReadOnlyMemory<char> textMemory,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var text = textMemory.Span;

            if (text.IsEmpty)
                return new ValueTask<IReadOnlyList<ISpellCheckResult>>(Array.Empty<ISpellCheckResult>());

            List<ISpellCheckResult>? results = null;
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
                    using var errors = _spellChecker.Check(textPtr);

                    while (true)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        void* errorPtr = null;
                        var hr = unchecked((int)errors.Next(&errorPtr));

                        if (hr < 0)
                            throw new COMException("IEnumSpellingError::Next failed", hr);

                        if (hr == S_FALSE || errorPtr is null)
                            break;

                        using var error = MicroComRuntime.CreateProxyFor<ISpellingError>(errorPtr, true);
                        var start = checked((int)error.StartIndex);
                        var length = checked((int)error.Length);

                        if (start < 0 || length <= 0 || start + length > text.Length)
                            continue;

                        var action = error.CorrectiveAction;

                        // A repeated word can only be deleted and has no suggestions to offer.
                        if (action == CorrectiveActionDelete)
                            continue;

                        var replacement = action == CorrectiveActionReplace
                            ? ReadAndFreeString(error.Replacement)
                            : null;

                        (results ??= new List<ISpellCheckResult>()).Add(new Win32Result(
                            this,
                            start,
                            length,
                            text.Slice(start, length).ToString(),
                            replacement));
                    }
                }
            }
            finally
            {
                if (rented is not null)
                    ArrayPool<char>.Shared.Return(rented);
            }

            return new ValueTask<IReadOnlyList<ISpellCheckResult>>(
                results is null ? Array.Empty<ISpellCheckResult>() : results);
        }

        public ValueTask<IReadOnlyList<string>> SuggestCoreAsync(
            string word,
            string? replacement,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            List<string>? results = null;

            if (!string.IsNullOrEmpty(replacement))
                results = new List<string> { replacement };

            fixed (char* wordPtr = word)
            {
                void* suggestionsPtr = null;
                var hr = unchecked((int)_spellChecker.Suggest(wordPtr, &suggestionsPtr));

                if (hr < 0)
                    throw new COMException("ISpellChecker::Suggest failed", hr);

                using var suggestions = MicroComRuntime.CreateProxyOrNullFor<IEnumString>(suggestionsPtr, true);

                if (hr != S_FALSE && suggestions is not null)
                    ReadStrings(suggestions, ref results, cancellationToken);
            }

            return new ValueTask<IReadOnlyList<string>>(
                results is null ? Array.Empty<string>() : results);
        }

    }

    private sealed class Win32Result : SpellCheckResultBase
    {
        private readonly Win32Context _context;
        private readonly string _word;
        private readonly string? _replacement;

        public Win32Result(Win32Context context, int start, int length, string word, string? replacement)
            : base(context, start, length)
        {
            _context = context;
            _word = word;
            _replacement = replacement;
        }

        // The autocorrect replacement, when present, is offered first.
        protected override ValueTask<IReadOnlyList<string>> SuggestCoreAsync(CancellationToken cancellationToken) =>
            _context.SuggestCoreAsync(_word, _replacement, cancellationToken);
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

    private static string? ReadAndFreeString(char* value)
    {
        if (value is null)
            return null;

        try
        {
            return Marshal.PtrToStringUni((IntPtr)value);
        }
        finally
        {
            Marshal.FreeCoTaskMem((IntPtr)value);
        }
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

    private string? GetSupportedLanguageTag(ISpellCheckerFactory factory, CultureInfo culture)
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

    private static IEnumerable<string> GetLanguageTagCandidates(CultureInfo culture)
    {
        foreach (var tag in GetLanguageTagCandidates(culture.Name, culture.TwoLetterISOLanguageName))
        {
            yield return tag;
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

}
