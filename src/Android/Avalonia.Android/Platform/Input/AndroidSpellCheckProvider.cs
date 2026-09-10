using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Runtime;
using Android.Views.TextService;
using Avalonia.Input.TextInput;
using Avalonia.Media.TextFormatting.Unicode;
using Locale = Java.Util.Locale;
using Object = Java.Lang.Object;
using TextServiceTextInfo = Android.Views.TextService.TextInfo;

namespace Avalonia.Android.Platform.Input;

internal sealed class AndroidSpellCheckProvider : ISpellCheckProvider
{
    private static readonly TimeSpan QueryTimeout = TimeSpan.FromSeconds(2);
    private const int MaxSuggestions = 8;

    private readonly TextServicesManager? _manager;

    public AndroidSpellCheckProvider(Context context)
    {
        _manager = context.GetSystemService(Context.TextServicesManagerService)
            ?.JavaCast<TextServicesManager>();
    }

    public bool IsLanguageSupported(CultureInfo? culture)
    {
        if (_manager is null)
        {
            return false;
        }

        if (!OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            return true;
        }

        return IsLanguageSupportedCore(culture);
    }

    [SupportedOSPlatform("android31.0")]
    private bool IsLanguageSupportedCore(CultureInfo? culture)
    {
        if (_manager is not { IsSpellCheckerEnabled: true } manager ||
            manager.CurrentSpellCheckerInfo is not { } spellCheckerInfo)
        {
            return false;
        }

        // A null culture uses the user's spell checker settings.
        if (culture is null || spellCheckerInfo.SubtypeCount == 0)
        {
            return true;
        }

        foreach (var languageTag in GetLanguageTagCandidates(culture))
        {
            for (var i = 0; i < spellCheckerInfo.SubtypeCount; i++)
            {
                var subtype = spellCheckerInfo.GetSubtypeAt(i);

#pragma warning disable CA1422 // Locale is obsolete, but Android spell checkers can still leave LanguageTag empty.
                if (subtype is not null &&
                    (Matches(languageTag, subtype.LanguageTag) ||
                        Matches(languageTag, subtype.Locale)))
#pragma warning restore CA1422
                {
                    return true;
                }
            }
        }

        return false;
    }

    public async ValueTask<IReadOnlyList<SpellCheckResult>> CheckAsync(
        ReadOnlyMemory<char> textMemory,
        CultureInfo? culture,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_manager is not { } manager || textMemory.IsEmpty)
        {
            return Array.Empty<SpellCheckResult>();
        }

        var text = textMemory.ToString();

        // Do not dispose the listener: late Java callbacks can abort the process if its peer is gone.
        var listener = new SessionListener();
        var session = CreateSession(manager, culture, listener);

        if (session is null)
        {
            // On API 30 and earlier, a disabled spell checker can return no session.
            return Array.Empty<SpellCheckResult>();
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(QueryTimeout);

            // Gboard can suppress the active word in sentence queries.
            if (TryGetSingleWord(text, out var start, out var length))
            {
                var words = await listener.GetWordSuggestionsAsync(
                    session, text.Substring(start, length), MaxSuggestions, timeout.Token);

                return words is { Length: > 0 } && words[0] is { } word && IsMisspelled(word)
                    ? new[] { new SpellCheckResult(start, length) }
                    : Array.Empty<SpellCheckResult>();
            }

            // No ConfigureAwait(false): the session is closed on the thread that created it.
            var results = await listener.GetSentenceSuggestionsAsync(session, text, MaxSuggestions, timeout.Token);

            return GetMisspellings(text, results);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("Android spell checking did not respond before the timeout.", ex);
        }
        finally
        {
            session.Close();
            session.Dispose();
        }
    }

    public async ValueTask<IReadOnlyList<string>> SuggestAsync(
        string word,
        CultureInfo? culture,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_manager is not { } manager ||
            string.IsNullOrWhiteSpace(word))
        {
            return Array.Empty<string>();
        }

        var listener = new SessionListener();
        var session = CreateSession(manager, culture, listener);

        if (session is null)
        {
            return Array.Empty<string>();
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(QueryTimeout);

            var results = await listener.GetWordSuggestionsAsync(session, word, MaxSuggestions, timeout.Token);

            return results is { Length: > 0 } ? GetSuggestions(results[0]) : Array.Empty<string>();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Array.Empty<string>();
        }
        finally
        {
            session.Close();
            session.Dispose();
        }
    }

    private static SpellCheckerSession? CreateSession(
        TextServicesManager manager,
        CultureInfo? culture,
        SessionListener listener)
    {
        if (culture is null || string.IsNullOrEmpty(culture.Name))
        {
            return manager.NewSpellCheckerSession(null, null, listener, true);
        }

        // An explicit culture overrides the user's current subtype.
        using var locale = Locale.ForLanguageTag(culture.Name);
        return manager.NewSpellCheckerSession(null, locale, listener, false);
    }

    private static IReadOnlyList<SpellCheckResult> GetMisspellings(
        string text,
        SentenceSuggestionsInfo[]? results)
    {
        if (results is null || results.Length == 0)
        {
            return Array.Empty<SpellCheckResult>();
        }

        var misspellings = new List<SpellCheckResult>();

        foreach (var sentence in results)
        {
            if (sentence is null)
            {
                continue;
            }

            for (var i = 0; i < sentence.SuggestionsCount; i++)
            {
                var suggestionInfo = sentence.GetSuggestionsInfoAt(i);
                var start = sentence.GetOffsetAt(i);
                var length = sentence.GetLengthAt(i);

                if (suggestionInfo is null ||
                    start < 0 ||
                    length <= 0 ||
                    start >= text.Length ||
                    !IsMisspelled(suggestionInfo))
                {
                    continue;
                }

                length = Math.Min(length, text.Length - start);
                misspellings.Add(new SpellCheckResult(start, length));
            }
        }

        return misspellings.Count == 0 ? Array.Empty<SpellCheckResult>() : misspellings;
    }

    private static bool TryGetSingleWord(string text, out int start, out int length)
    {
        start = 0;
        var end = text.Length;

        while (start < end && (char.IsWhiteSpace(text[start]) || char.IsPunctuation(text[start])))
            start++;
        while (end > start && (char.IsWhiteSpace(text[end - 1]) || char.IsPunctuation(text[end - 1])))
            end--;

        length = end - start;
        if (length == 0 || !char.IsLetter(text, start) ||
            SpellCheckTokenization.RequiresWholeToken(text.AsSpan()))
            return false;

        var words = new WordBreakEnumerator(text.AsSpan(start, length));
        return words.MoveNext(out var word) && word.Length == length;
    }

    private static IReadOnlyList<string> GetSuggestions(SuggestionsInfo? suggestionInfo)
    {
        if (suggestionInfo is null || suggestionInfo.SuggestionsCount <= 0)
            return Array.Empty<string>();

        var suggestions = new List<string>(suggestionInfo.SuggestionsCount);

        for (var i = 0; i < suggestionInfo.SuggestionsCount; i++)
        {
            var suggestion = suggestionInfo.GetSuggestionAt(i);

            if (!string.IsNullOrEmpty(suggestion))
                suggestions.Add(suggestion);
        }

        return suggestions.Count == 0 ? Array.Empty<string>() : suggestions;
    }

    private static bool IsMisspelled(SuggestionsInfo suggestionsInfo)
    {
        var attributes = suggestionsInfo.SuggestionsAttributes;
        var isInDictionary = (attributes & (int)SuggestionsAttributes.InTheDictionary) != 0;
        var looksWrong =
            (attributes & (int)SuggestionsAttributes.LooksLikeTypo) != 0 ||
            (attributes & (int)SuggestionsAttributes.HasRecommendedSuggestions) != 0 ||
            (OperatingSystem.IsAndroidVersionAtLeast(31) &&
                (attributes & (int)SuggestionsAttributes.LooksLikeGrammarError) != 0);

        return !isInDictionary && looksWrong;
    }

    private static IEnumerable<string> GetLanguageTagCandidates(CultureInfo culture)
    {
        var languageTag = culture.Name;

        if (!string.IsNullOrEmpty(languageTag))
        {
            yield return languageTag;

            var underscoreLanguageTag = languageTag.Replace('-', '_');

            if (underscoreLanguageTag != languageTag)
            {
                yield return underscoreLanguageTag;
            }
        }

        var neutralLanguageTag = culture.TwoLetterISOLanguageName;

        if (!string.IsNullOrEmpty(neutralLanguageTag) &&
            !string.Equals(neutralLanguageTag, languageTag, StringComparison.OrdinalIgnoreCase))
        {
            yield return neutralLanguageTag;
        }
    }

    private static bool Matches(string expected, string? actual)
    {
        if (string.IsNullOrEmpty(actual))
        {
            return false;
        }

        if (string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(expected.Replace('-', '_'), actual, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(expected.Replace('_', '-'), actual, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Match Android's fallback to another subtype of the same language.
        return string.Equals(LanguagePart(expected), LanguagePart(actual), StringComparison.OrdinalIgnoreCase);
    }

    private static string LanguagePart(string tag)
    {
        var separator = tag.IndexOfAny(new[] { '-', '_' });
        return separator > 0 ? tag.Substring(0, separator) : tag;
    }

    private sealed class SessionListener : Object, SpellCheckerSession.ISpellCheckerSessionListener
    {
        private TaskCompletionSource<SentenceSuggestionsInfo[]?>? _sentenceSuggestions;
        private TaskCompletionSource<SuggestionsInfo[]?>? _wordSuggestions;

        public SessionListener()
        {
        }

        // Allow Java.Interop to recreate the managed peer for late callbacks.
        public SessionListener(IntPtr handle, JniHandleOwnership transfer)
            : base(handle, transfer)
        {
        }

        public Task<SentenceSuggestionsInfo[]?> GetSentenceSuggestionsAsync(
            SpellCheckerSession session,
            string text,
            int suggestionsLimit,
            CancellationToken cancellationToken)
        {
            _sentenceSuggestions = new TaskCompletionSource<SentenceSuggestionsInfo[]?>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            var registration = cancellationToken.Register(() =>
            {
                session.Cancel();
                _sentenceSuggestions.TrySetCanceled(cancellationToken);
            });

            _sentenceSuggestions.Task.ContinueWith(
                _ => registration.Dispose(),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);

            session.GetSentenceSuggestions([new TextServiceTextInfo(text)], suggestionsLimit);
            return _sentenceSuggestions.Task;
        }

        public void OnGetSentenceSuggestions(SentenceSuggestionsInfo[]? results)
        {
            _sentenceSuggestions?.TrySetResult(results);
        }

        public void OnGetSuggestions(SuggestionsInfo[]? results)
        {
            _wordSuggestions?.TrySetResult(results);
        }

        public Task<SuggestionsInfo[]?> GetWordSuggestionsAsync(
            SpellCheckerSession session,
            string word,
            int suggestionsLimit,
            CancellationToken cancellationToken)
        {
            _wordSuggestions = new TaskCompletionSource<SuggestionsInfo[]?>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            var registration = cancellationToken.Register(() =>
            {
                session.Cancel();
                _wordSuggestions.TrySetCanceled(cancellationToken);
            });

            _wordSuggestions.Task.ContinueWith(
                _ => registration.Dispose(),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);

#pragma warning disable CS0618 // Sentence queries can omit the word being edited.
            session.GetSuggestions(new TextServiceTextInfo(word), suggestionsLimit);
#pragma warning restore CS0618
            return _wordSuggestions.Task;
        }
    }
}
