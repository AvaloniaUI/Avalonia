using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Input.TextInput;
using Avalonia.Media.TextFormatting.Unicode;

namespace Avalonia.FreeDesktop;

internal sealed unsafe class EnchantSpellCheckProvider : ISpellCheckProvider, IDisposable
{
    private const string EnchantLibrary = "libenchant-2.so.2";

    private readonly Dictionary<string, IntPtr> _dictionaries = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _unsupportedDictionaries = new(StringComparer.OrdinalIgnoreCase);
    private IntPtr _broker;
    private bool _unavailable;

    ~EnchantSpellCheckProvider()
    {
        ReleaseNativeResources();
    }

    public void Dispose()
    {
        ReleaseNativeResources();
        GC.SuppressFinalize(this);
    }

    private void ReleaseNativeResources()
    {
        foreach (var dictionary in _dictionaries.Values)
        {
            EnchantBrokerFreeDict(_broker, dictionary);
        }

        _dictionaries.Clear();

        if (_broker != IntPtr.Zero)
        {
            EnchantBrokerFree(_broker);
            _broker = IntPtr.Zero;
        }

        _unavailable = true;
    }

    public bool IsLanguageSupported(CultureInfo? culture)
    {
        return GetDictionary(culture) != IntPtr.Zero;
    }

    public ValueTask<IReadOnlyList<SpellCheckResult>> CheckAsync(
        ReadOnlySpan<char> text,
        CultureInfo? culture,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dictionary = GetDictionary(culture);

        if (dictionary == IntPtr.Zero || text.IsEmpty || IsWhiteSpace(text))
        {
            return new ValueTask<IReadOnlyList<SpellCheckResult>>(Array.Empty<SpellCheckResult>());
        }

        var results = new List<SpellCheckResult>();
        var wordBreaker = new WordBreakEnumerator(text);

        while (wordBreaker.MoveNext(out var segment))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var word = text.Slice(segment.Offset, segment.Length);

            if (!IsCheckableWord(word))
            {
                continue;
            }

            if (!IsWordCorrect(dictionary, word))
            {
                results.Add(new SpellCheckResult(segment.Offset, segment.Length, word.ToString()));
            }
        }

        return new ValueTask<IReadOnlyList<SpellCheckResult>>(
            results.Count == 0 ? Array.Empty<SpellCheckResult>() : results);
    }

    public ValueTask<IReadOnlyList<string>> SuggestAsync(
        string word,
        CultureInfo? culture,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dictionary = GetDictionary(culture);

        if (dictionary == IntPtr.Zero || string.IsNullOrWhiteSpace(word))
        {
            return new ValueTask<IReadOnlyList<string>>(Array.Empty<string>());
        }

        using var utf8Word = new Utf8String(word);
        nuint count = 0;
        var suggestions = EnchantDictSuggest(dictionary, utf8Word.Pointer, utf8Word.Length, &count);

        if (suggestions == IntPtr.Zero || count == 0)
        {
            return new ValueTask<IReadOnlyList<string>>(Array.Empty<string>());
        }

        try
        {
            var length = checked((int)count);
            var pointers = new IntPtr[length];
            Marshal.Copy(suggestions, pointers, 0, length);

            var results = new List<string>(length);

            foreach (var pointer in pointers)
            {
                var value = PtrToStringUtf8(pointer);

                if (!string.IsNullOrEmpty(value))
                {
                    results.Add(value);
                }
            }

            return new ValueTask<IReadOnlyList<string>>(results);
        }
        finally
        {
            EnchantDictFreeStringList(dictionary, suggestions);
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

    private static bool IsCheckableWord(ReadOnlySpan<char> word)
    {
        for (var i = 0; i < word.Length;)
        {
            var codepoint = Codepoint.ReadAt(word, i, out var count);

            if (codepoint.GeneralCategory is
                GeneralCategory.Letter or
                GeneralCategory.CasedLetter or
                GeneralCategory.LowercaseLetter or
                GeneralCategory.ModifierLetter or
                GeneralCategory.OtherLetter or
                GeneralCategory.TitlecaseLetter or
                GeneralCategory.UppercaseLetter or
                GeneralCategory.Mark or
                GeneralCategory.SpacingMark or
                GeneralCategory.EnclosingMark or
                GeneralCategory.NonspacingMark)
            {
                return true;
            }

            i += count;
        }

        return false;
    }

    private bool IsWordCorrect(IntPtr dictionary, ReadOnlySpan<char> word)
    {
        using var utf8Word = new Utf8String(word);
        return EnchantDictCheck(dictionary, utf8Word.Pointer, utf8Word.Length) == 0;
    }

    private IntPtr GetDictionary(CultureInfo? culture)
    {
        if (_unavailable)
        {
            return IntPtr.Zero;
        }

        EnsureBroker();

        if (_broker == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        foreach (var languageTag in GetLanguageTags(culture))
        {
            if (_dictionaries.TryGetValue(languageTag, out var dictionary))
            {
                return dictionary;
            }

            if (_unsupportedDictionaries.Contains(languageTag))
            {
                continue;
            }

            using var utf8LanguageTag = new Utf8String(languageTag);
            dictionary = EnchantBrokerRequestDict(_broker, utf8LanguageTag.Pointer);

            if (dictionary != IntPtr.Zero)
            {
                _dictionaries[languageTag] = dictionary;
                return dictionary;
            }

            _unsupportedDictionaries.Add(languageTag);
        }

        return IntPtr.Zero;
    }

    private void EnsureBroker()
    {
        if (_broker != IntPtr.Zero || _unavailable)
        {
            return;
        }

        try
        {
            _broker = EnchantBrokerInit();
        }
        catch (DllNotFoundException)
        {
            _unavailable = true;
        }
        catch (EntryPointNotFoundException)
        {
            _unavailable = true;
        }
        catch (BadImageFormatException)
        {
            _unavailable = true;
        }
    }

    private static IEnumerable<string> GetLanguageTags(CultureInfo? culture)
    {
        var cultureName = culture?.Name;

        if (string.IsNullOrEmpty(cultureName))
        {
            cultureName = CultureInfo.CurrentCulture.Name;
        }

        if (string.IsNullOrEmpty(cultureName))
        {
            yield break;
        }

        yield return cultureName;

        var underscoreName = cultureName.Replace('-', '_');

        if (!string.Equals(underscoreName, cultureName, StringComparison.Ordinal))
        {
            yield return underscoreName;
        }

        var neutralName = culture?.TwoLetterISOLanguageName;

        if (string.IsNullOrEmpty(neutralName))
        {
            try
            {
                neutralName = new CultureInfo(cultureName).TwoLetterISOLanguageName;
            }
            catch (CultureNotFoundException)
            {
            }
        }

        if (!string.IsNullOrEmpty(neutralName) &&
            !string.Equals(neutralName, cultureName, StringComparison.OrdinalIgnoreCase))
        {
            yield return neutralName;
        }
    }

    private static string? PtrToStringUtf8(IntPtr pointer)
    {
        if (pointer == IntPtr.Zero)
        {
            return null;
        }

        var length = 0;
        var bytes = (byte*)pointer;

        while (bytes[length] != 0)
        {
            length++;
        }

        return Encoding.UTF8.GetString(bytes, length);
    }

    [DllImport(EnchantLibrary, EntryPoint = "enchant_broker_init", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr EnchantBrokerInit();

    [DllImport(EnchantLibrary, EntryPoint = "enchant_broker_free", CallingConvention = CallingConvention.Cdecl)]
    private static extern void EnchantBrokerFree(IntPtr broker);

    [DllImport(EnchantLibrary, EntryPoint = "enchant_broker_request_dict", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr EnchantBrokerRequestDict(IntPtr broker, byte* languageTag);

    [DllImport(EnchantLibrary, EntryPoint = "enchant_broker_free_dict", CallingConvention = CallingConvention.Cdecl)]
    private static extern void EnchantBrokerFreeDict(IntPtr broker, IntPtr dictionary);

    [DllImport(EnchantLibrary, EntryPoint = "enchant_dict_check", CallingConvention = CallingConvention.Cdecl)]
    private static extern int EnchantDictCheck(IntPtr dictionary, byte* word, nint length);

    [DllImport(EnchantLibrary, EntryPoint = "enchant_dict_suggest", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr EnchantDictSuggest(IntPtr dictionary, byte* word, nint length, nuint* count);

    [DllImport(EnchantLibrary, EntryPoint = "enchant_dict_free_string_list", CallingConvention = CallingConvention.Cdecl)]
    private static extern void EnchantDictFreeStringList(IntPtr dictionary, IntPtr stringList);

    private sealed class Utf8String : IDisposable
    {
        private readonly GCHandle _handle;

        public Utf8String(ReadOnlySpan<char> value)
        {
            var nullTerminated = new byte[Encoding.UTF8.GetByteCount(value) + 1];
            Length = Encoding.UTF8.GetBytes(value, nullTerminated);
            _handle = GCHandle.Alloc(nullTerminated, GCHandleType.Pinned);
            Pointer = (byte*)_handle.AddrOfPinnedObject();
        }

        public byte* Pointer { get; }

        public int Length { get; }

        public void Dispose()
        {
            if (_handle.IsAllocated)
            {
                _handle.Free();
            }
        }
    }
}
