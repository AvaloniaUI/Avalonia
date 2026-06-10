using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Input.TextInput;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Globalization;
using Windows.Win32.System.Com;

namespace Avalonia.Win32.Input;

internal sealed unsafe class Win32SpellCheckProvider : ISpellCheckProvider, IDisposable
{
    private readonly Dictionary<string, ISpellChecker> _spellCheckers = new(StringComparer.OrdinalIgnoreCase);
    private ISpellCheckerFactory? _factory;
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var spellChecker in _spellCheckers.Values)
        {
            ReleaseComObject(spellChecker);
        }

        _spellCheckers.Clear();
        ReleaseComObject(_factory);
        _factory = null;
    }

    public bool IsLanguageSupported(CultureInfo? culture)
    {
        if (GetFactory() is not { } factory ||
            GetSupportedLanguageTag(factory, culture) is null)
        {
            return false;
        }

        return true;
    }

    public ValueTask<IReadOnlyList<SpellCheckResult>> CheckAsync(
        string text,
        CultureInfo? culture,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrEmpty(text) ||
            GetSpellChecker(culture) is not { } spellChecker)
        {
            return new ValueTask<IReadOnlyList<SpellCheckResult>>(Array.Empty<SpellCheckResult>());
        }

        var results = new List<SpellCheckResult>();

        fixed (char* textPtr = text)
        {
            var errors = spellChecker.Check(textPtr);

            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var hr = errors.Next(out var error);

                    if (hr == HRESULT.S_FALSE)
                    {
                        break;
                    }

                    hr.ThrowOnFailure();

                    try
                    {
                        var start = checked((int)error.StartIndex);
                        var length = checked((int)error.Length);
                        var word = start >= 0 && length > 0 && start + length <= text.Length
                            ? text.Substring(start, length)
                            : null;

                        results.Add(new SpellCheckResult(start, length, word));
                    }
                    finally
                    {
                        ReleaseComObject(error);
                    }
                }
            }
            finally
            {
                ReleaseComObject(errors);
            }
        }

        return new ValueTask<IReadOnlyList<SpellCheckResult>>(results);
    }

    public ValueTask<IReadOnlyList<string>> SuggestAsync(
        string word,
        CultureInfo? culture,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(word) ||
            GetSpellChecker(culture) is not { } spellChecker)
        {
            return new ValueTask<IReadOnlyList<string>>(Array.Empty<string>());
        }

        var results = new List<string>();

        fixed (char* wordPtr = word)
        {
            var suggestions = spellChecker.Suggest(wordPtr);

            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    PWSTR suggestion = default;
                    uint fetched = 0;
                    var hr = suggestions.Next(1, &suggestion, &fetched);

                    if (hr == HRESULT.S_FALSE || fetched == 0)
                    {
                        break;
                    }

                    hr.ThrowOnFailure();

                    try
                    {
                        var value = suggestion.ToString();

                        if (!string.IsNullOrEmpty(value))
                        {
                            results.Add(value);
                        }
                    }
                    finally
                    {
                        PInvoke.CoTaskMemFree(suggestion.Value);
                    }
                }
            }
            finally
            {
                ReleaseComObject(suggestions);
            }
        }

        return new ValueTask<IReadOnlyList<string>>(results);
    }

    private ISpellChecker? GetSpellChecker(CultureInfo? culture)
    {
        if (_disposed)
        {
            return null;
        }

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

    private ISpellCheckerFactory? GetFactory()
    {
        if (_disposed)
        {
            return null;
        }

        if (_factory is not null)
        {
            return _factory;
        }

        if (Win32Platform.WindowsVersion < PlatformConstants.Windows8 ||
            OleContext.Current is null)
        {
            return null;
        }

        var clsid = typeof(SpellCheckerFactory).GUID;
        var iid = typeof(ISpellCheckerFactory).GUID;
        var hr = CoCreateInstance(
            in clsid,
            IntPtr.Zero,
            CLSCTX.CLSCTX_INPROC_SERVER,
            in iid,
            out var factoryPtr);

        if (hr.Failed || factoryPtr == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            return _factory = (ISpellCheckerFactory)Marshal.GetObjectForIUnknown(factoryPtr);
        }
        finally
        {
            Marshal.Release(factoryPtr);
        }
    }

    private static string? GetSupportedLanguageTag(ISpellCheckerFactory factory, CultureInfo? culture)
    {
        foreach (var languageTag in GetLanguageTagCandidates(culture))
        {
            fixed (char* languageTagPtr = languageTag)
            {
                if (factory.IsSupported(languageTagPtr))
                {
                    return languageTag;
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

        if (!string.IsNullOrEmpty(neutralLanguageTag) &&
            !string.Equals(neutralLanguageTag, languageTag, StringComparison.OrdinalIgnoreCase))
        {
            yield return neutralLanguageTag;
        }
    }

    private static void ReleaseComObject(object? comObject)
    {
        if (comObject is not null && Marshal.IsComObject(comObject))
        {
            Marshal.ReleaseComObject(comObject);
        }
    }

    [DllImport("ole32.dll", ExactSpelling = true)]
    private static extern HRESULT CoCreateInstance(
        in Guid rclsid,
        IntPtr pUnkOuter,
        CLSCTX dwClsContext,
        in Guid riid,
        out IntPtr ppv);
}
