using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Input.TextInput;
using Avalonia.Media.TextFormatting.Unicode;

namespace Avalonia.FreeDesktop;

/// <summary>
/// Spell checking through libenchant-2.
/// </summary>
/// <remarks>
/// Share the broker and dictionaries to avoid repeated loads. Contexts keep all dictionary access on their
/// creating UI thread; native resources live until process exit.
/// </remarks>
internal sealed unsafe class EnchantSpellCheckProvider : ISpellCheckProvider
{
    private const string EnchantLibrary = "libenchant-2.so.2";
    private const int StackallocByteLimit = 512;

    public static EnchantSpellCheckProvider Instance { get; } = new();

    private readonly Dictionary<string, IntPtr> _dictionaries = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _unsupportedDictionaries = new(StringComparer.OrdinalIgnoreCase);
    private IntPtr _broker;
    private bool _unavailable;
    private EnchantSpellCheckProvider()
    {
    }

    public IReadOnlyList<CultureInfo> SupportedCultures
    {
        get
        {
            EnsureBroker();

            if (_broker == IntPtr.Zero)
                return Array.Empty<CultureInfo>();

            var languageTags = new List<string>();
            var handle = GCHandle.Alloc(languageTags);

            try
            {
                EnchantBrokerListDicts(_broker, s_dictionaryCallback, GCHandle.ToIntPtr(handle));
            }
            finally
            {
                handle.Free();
            }

            var cultures = new List<CultureInfo>(languageTags.Count);

            foreach (var languageTag in languageTags)
            {
                try
                {
                    cultures.Add(CultureInfo.GetCultureInfo(languageTag.Replace('_', '-')));
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
        var dictionary = GetDictionary(culture);
        return dictionary == IntPtr.Zero ? null : new EnchantContext(dictionary);
    }

    private sealed class EnchantContext : SpellCheckContextBase
    {
        private readonly IntPtr _dictionary;

        public EnchantContext(IntPtr dictionary)
        {
            _dictionary = dictionary;
        }

        protected override ValueTask<IReadOnlyList<ISpellCheckResult>> CheckCoreAsync(
            ReadOnlyMemory<char> textMemory,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var text = textMemory.Span;

            if (text.IsEmpty || IsWhiteSpace(text))
                return new ValueTask<IReadOnlyList<ISpellCheckResult>>(Array.Empty<ISpellCheckResult>());

            List<ISpellCheckResult>? results = null;
            var words = new CheckableWordEnumerator(text);

            while (words.MoveNext(out var offset, out var length))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var word = text.Slice(offset, length);

                if (!IsWordCorrect(_dictionary, word))
                {
                    (results ??= new List<ISpellCheckResult>()).Add(
                        new EnchantResult(this, offset, length, word.ToString()));
                }
            }

            return new ValueTask<IReadOnlyList<ISpellCheckResult>>(
                results is null ? Array.Empty<ISpellCheckResult>() : results);
        }

        public ValueTask<IReadOnlyList<string>> SuggestCoreAsync(
            string word,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(word))
                return new ValueTask<IReadOnlyList<string>>(Array.Empty<string>());

            byte[]? rented = null;

            try
            {
                var utf8Word = EncodeUtf8(word, stackalloc byte[StackallocByteLimit], ref rented, out var byteLength);
                nuint count = 0;
                IntPtr suggestions;

                fixed (byte* wordPtr = utf8Word)
                    suggestions = EnchantDictSuggest(_dictionary, wordPtr, byteLength, &count);

                if (suggestions == IntPtr.Zero || count == 0)
                    return new ValueTask<IReadOnlyList<string>>(Array.Empty<string>());

                try
                {
                    var length = checked((int)count);
                    var pointers = (IntPtr*)suggestions;
                    var results = new List<string>(length);

                    for (var i = 0; i < length; i++)
                    {
                        var value = PtrToStringUtf8(pointers[i]);

                        if (!string.IsNullOrEmpty(value))
                            results.Add(value);
                    }

                    return new ValueTask<IReadOnlyList<string>>(results);
                }
                finally
                {
                    EnchantDictFreeStringList(_dictionary, suggestions);
                }
            }
            finally
            {
                if (rented is not null)
                    ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    private sealed class EnchantResult : SpellCheckResultBase
    {
        private readonly EnchantContext _context;
        private readonly string _word;

        public EnchantResult(EnchantContext context, int start, int length, string word)
            : base(context, start, length)
        {
            _context = context;
            _word = word;
        }

        protected override ValueTask<IReadOnlyList<string>> SuggestCoreAsync(CancellationToken cancellationToken) =>
            _context.SuggestCoreAsync(_word, cancellationToken);
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

    // Filter whole tokens before word breaking strips address and identifier separators.
    internal ref struct CheckableWordEnumerator
    {
        private readonly ReadOnlySpan<char> _text;
        private WordBreakEnumerator _wordBreaker;
        private int _tokenStart;
        private int _nextTokenStart;

        public CheckableWordEnumerator(ReadOnlySpan<char> text)
        {
            _text = text;
            _wordBreaker = default;
            _tokenStart = 0;
            _nextTokenStart = 0;
        }

        public bool MoveNext(out int offset, out int length)
        {
            while (true)
            {
                while (_wordBreaker.MoveNext(out var segment))
                {
                    offset = _tokenStart + segment.Offset;
                    length = segment.Length;

                    if (IsCheckableWord(_text.Slice(offset, length)))
                    {
                        return true;
                    }
                }

                while (_nextTokenStart < _text.Length && SpellCheckTokenization.IsBoundary(_text[_nextTokenStart]))
                {
                    _nextTokenStart++;
                }

                if (_nextTokenStart == _text.Length)
                {
                    offset = length = 0;
                    return false;
                }

                _tokenStart = _nextTokenStart;

                while (_nextTokenStart < _text.Length && !SpellCheckTokenization.IsBoundary(_text[_nextTokenStart]))
                {
                    _nextTokenStart++;
                }

                var token = _text.Slice(_tokenStart, _nextTokenStart - _tokenStart);

                if (!SpellCheckTokenization.RequiresWholeToken(token))
                {
                    _wordBreaker = new WordBreakEnumerator(token);
                }
            }
        }
    }

    private static bool IsCheckableWord(ReadOnlySpan<char> word)
    {
        var hasLetter = false;

        for (var i = 0; i < word.Length;)
        {
            var codepoint = Codepoint.ReadAt(word, i, out var count);

            switch (codepoint.GeneralCategory)
            {
                case GeneralCategory.Letter:
                case GeneralCategory.CasedLetter:
                case GeneralCategory.LowercaseLetter:
                case GeneralCategory.ModifierLetter:
                case GeneralCategory.OtherLetter:
                case GeneralCategory.TitlecaseLetter:
                case GeneralCategory.UppercaseLetter:
                    hasLetter = true;
                    break;
                case GeneralCategory.DecimalNumber:
                case GeneralCategory.LetterNumber:
                case GeneralCategory.OtherNumber:
                    return false;
            }

            if (count == 1)
            {
                var c = word[i];

                if (c is '@' or '/' or ':' or '\\' or '_' || (c == '.' && i > 0 && i < word.Length - 1))
                {
                    return false;
                }
            }

            i += count;
        }

        return hasLetter;
    }

    private static bool IsWordCorrect(IntPtr dictionary, ReadOnlySpan<char> word)
    {
        byte[]? rented = null;

        try
        {
            var utf8Word = EncodeUtf8(word, stackalloc byte[StackallocByteLimit], ref rented, out var byteLength);

            fixed (byte* wordPtr = utf8Word)
            {
                // 0 = correct, >0 = misspelled, <0 = error. An error must not produce an underline.
                return EnchantDictCheck(dictionary, wordPtr, byteLength) <= 0;
            }
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    // Use the supplied buffer or a pooled array for NUL-terminated UTF-8. The caller returns the array.
    private static Span<byte> EncodeUtf8(ReadOnlySpan<char> value, Span<byte> buffer, ref byte[]? rented, out int byteLength)
    {
        var required = Encoding.UTF8.GetByteCount(value) + 1;

        if (required > buffer.Length)
        {
            rented = ArrayPool<byte>.Shared.Rent(required);
            buffer = rented;
        }

        byteLength = Encoding.UTF8.GetBytes(value, buffer);
        buffer[byteLength] = 0;

        return buffer.Slice(0, byteLength + 1);
    }

    private IntPtr GetDictionary(CultureInfo culture)
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

        Span<byte> tagBuffer = stackalloc byte[64];

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

            byte[]? rented = null;

            try
            {
                var utf8Tag = EncodeUtf8(languageTag, tagBuffer, ref rented, out _);

                fixed (byte* tagPtr = utf8Tag)
                {
                    dictionary = EnchantBrokerRequestDict(_broker, tagPtr);
                }
            }
            finally
            {
                if (rented is not null)
                {
                    ArrayPool<byte>.Shared.Return(rented);
                }
            }

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

    private static IEnumerable<string> GetLanguageTags(CultureInfo culture)
    {
        foreach (var tag in GetLanguageTags(culture.Name, culture.TwoLetterISOLanguageName))
        {
            yield return tag;
        }
    }

    private static IEnumerable<string> GetLanguageTags(string cultureName, string? neutralName)
    {
        if (string.IsNullOrEmpty(cultureName))
        {
            // Enchant requires a non-empty language tag.
            yield break;
        }

        yield return cultureName;

        var underscoreName = cultureName.Replace('-', '_');

        if (!string.Equals(underscoreName, cultureName, StringComparison.Ordinal))
        {
            yield return underscoreName;
        }

        if (string.IsNullOrEmpty(neutralName))
        {
            var separator = cultureName.IndexOfAny(new[] { '-', '_' });
            neutralName = separator > 0 ? cultureName.Substring(0, separator) : null;
        }

        if (!string.IsNullOrEmpty(neutralName) &&
            !string.Equals(neutralName, cultureName, StringComparison.OrdinalIgnoreCase))
        {
            yield return neutralName;
        }
    }

    private static readonly EnchantDictDescribeCallback s_dictionaryCallback = OnDictionary;

    private static void OnDictionary(
        IntPtr languageTag,
        IntPtr providerName,
        IntPtr providerDescription,
        IntPtr providerFile,
        IntPtr userData)
    {
        var value = PtrToStringUtf8(languageTag);

        if (!string.IsNullOrEmpty(value) &&
            GCHandle.FromIntPtr(userData).Target is List<string> languageTags)
        {
            languageTags.Add(value);
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

    [DllImport(EnchantLibrary, EntryPoint = "enchant_broker_request_dict", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr EnchantBrokerRequestDict(IntPtr broker, byte* languageTag);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void EnchantDictDescribeCallback(
        IntPtr languageTag,
        IntPtr providerName,
        IntPtr providerDescription,
        IntPtr providerFile,
        IntPtr userData);

    [DllImport(EnchantLibrary, EntryPoint = "enchant_broker_list_dicts", CallingConvention = CallingConvention.Cdecl)]
    private static extern void EnchantBrokerListDicts(
        IntPtr broker,
        EnchantDictDescribeCallback callback,
        IntPtr userData);

    [DllImport(EnchantLibrary, EntryPoint = "enchant_dict_check", CallingConvention = CallingConvention.Cdecl)]
    private static extern int EnchantDictCheck(IntPtr dictionary, byte* word, nint length);

    [DllImport(EnchantLibrary, EntryPoint = "enchant_dict_suggest", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr EnchantDictSuggest(IntPtr dictionary, byte* word, nint length, nuint* count);

    [DllImport(EnchantLibrary, EntryPoint = "enchant_dict_free_string_list", CallingConvention = CallingConvention.Cdecl)]
    private static extern void EnchantDictFreeStringList(IntPtr dictionary, IntPtr stringList);

}
