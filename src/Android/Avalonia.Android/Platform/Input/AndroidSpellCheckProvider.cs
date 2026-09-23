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

    public IReadOnlyList<CultureInfo> SupportedCultures
    {
        get
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(31) ||
                _manager?.CurrentSpellCheckerInfo is not { } spellCheckerInfo)
            {
                return Array.Empty<CultureInfo>();
            }

            var cultures = new List<CultureInfo>(spellCheckerInfo.SubtypeCount);

            for (var i = 0; i < spellCheckerInfo.SubtypeCount; i++)
            {
                var subtype = spellCheckerInfo.GetSubtypeAt(i);
#pragma warning disable CA1422
                var tag = subtype?.LanguageTag;
                if (string.IsNullOrEmpty(tag))
                    tag = subtype?.Locale;
#pragma warning restore CA1422

                if (!string.IsNullOrEmpty(tag))
                {
                    try
                    {
                        cultures.Add(CultureInfo.GetCultureInfo(tag.Replace('_', '-')));
                    }
                    catch (CultureNotFoundException)
                    {
                    }
                }
            }

            return cultures;
        }
    }

    public ISpellCheckContext? CreateContext(CultureInfo culture)
    {
        if (_manager is not { } manager ||
            (OperatingSystem.IsAndroidVersionAtLeast(31) && !IsLanguageSupportedCore(culture)))
        {
            return null;
        }

        // Keep the listener alive for the complete native session. Disposing it while a late callback is pending
        // can invalidate the Java peer.
        var listener = new SessionListener();
        var nativeSession = CreateSession(manager, culture, listener);
        return nativeSession is null ? null : new AndroidContext(manager, culture, nativeSession, listener);
    }

    [SupportedOSPlatform("android31.0")]
    private bool IsLanguageSupportedCore(CultureInfo culture)
    {
        if (_manager is not { IsSpellCheckerEnabled: true } manager ||
            manager.CurrentSpellCheckerInfo is not { } spellCheckerInfo)
        {
            return false;
        }

        if (spellCheckerInfo.SubtypeCount == 0)
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

    private sealed class AndroidContext : SpellCheckContextBase
    {
        private readonly TextServicesManager _manager;
        private readonly CultureInfo _culture;
        private SpellCheckerSession? _nativeSession;
        private SessionListener _listener;

        public AndroidContext(
            TextServicesManager manager,
            CultureInfo culture,
            SpellCheckerSession nativeSession,
            SessionListener listener)
        {
            _manager = manager;
            _culture = culture;
            _nativeSession = nativeSession;
            _listener = listener;
        }

        protected override async ValueTask<IReadOnlyList<ISpellCheckResult>> CheckCoreAsync(
            ReadOnlyMemory<char> textMemory,
            CancellationToken cancellationToken)
        {
            if (textMemory.IsEmpty)
                return Array.Empty<ISpellCheckResult>();

            if (!EnsureNativeSession())
                return Array.Empty<ISpellCheckResult>();

            var nativeSession = _nativeSession!;
            var listener = _listener;
            var text = textMemory.ToString();

            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(QueryTimeout);

                // Gboard can suppress the active word in sentence queries.
                if (TryGetSingleWord(text, out var start, out var length))
                {
                    var words = await listener.GetWordSuggestionsAsync(
                        nativeSession, text.Substring(start, length), MaxSuggestions, timeout.Token);

                    return words is { Length: > 0 } && words[0] is { } word && IsMisspelled(word)
                        ? new[] { new AndroidResult(this, start, length, word) }
                        : Array.Empty<ISpellCheckResult>();
                }

                var results = await listener.GetSentenceSuggestionsAsync(
                    nativeSession, text, MaxSuggestions, timeout.Token);

                return GetMisspellings(this, text, results);
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException("Android spell checking did not respond before the timeout.", ex);
            }
        }

        protected override void DisposeCore()
        {
            CloseNativeSession();
        }

        private bool EnsureNativeSession()
        {
            if (!_listener.WasCanceled)
                return _nativeSession is not null;

            // Android may deliver a late callback after Cancel. Move the next request to a fresh
            // listener/session so that callback cannot complete the new operation.
            CloseNativeSession();
            _listener = new SessionListener();
            _nativeSession = CreateSession(_manager, _culture, _listener);
            return _nativeSession is not null;
        }

        private void CloseNativeSession()
        {
            if (_nativeSession is not { } nativeSession)
                return;

            _nativeSession = null;
            nativeSession.Close();
            nativeSession.Dispose();
        }
    }

    // Android returns suggestions with the check, so no second native call is needed.
    private sealed class AndroidResult : SpellCheckResultBase
    {
        private readonly IReadOnlyList<string> _suggestions;

        public AndroidResult(AndroidContext context, int start, int length, SuggestionsInfo info)
            : base(context, start, length)
        {
            _suggestions = GetSuggestions(info);
        }

        protected override ValueTask<IReadOnlyList<string>> SuggestCoreAsync(CancellationToken cancellationToken) =>
            new(_suggestions);
    }

    private static SpellCheckerSession? CreateSession(
        TextServicesManager manager,
        CultureInfo culture,
        SessionListener listener)
    {
        if (string.IsNullOrEmpty(culture.Name))
        {
            return manager.NewSpellCheckerSession(null, null, listener, true);
        }

        // An explicit culture overrides the user's current subtype.
        using var locale = Locale.ForLanguageTag(culture.Name);
        return manager.NewSpellCheckerSession(null, locale, listener, false);
    }

    private static IReadOnlyList<ISpellCheckResult> GetMisspellings(
        AndroidContext context,
        string text,
        SentenceSuggestionsInfo[]? results)
    {
        if (results is null || results.Length == 0)
        {
            return Array.Empty<ISpellCheckResult>();
        }

        var misspellings = new List<ISpellCheckResult>();

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
                misspellings.Add(new AndroidResult(context, start, length, suggestionInfo));
            }
        }

        return misspellings.Count == 0 ? Array.Empty<ISpellCheckResult>() : misspellings;
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

        public bool WasCanceled { get; private set; }

        public Task<SentenceSuggestionsInfo[]?> GetSentenceSuggestionsAsync(
            SpellCheckerSession session,
            string text,
            int suggestionsLimit,
            CancellationToken cancellationToken)
        {
            var completion = new TaskCompletionSource<SentenceSuggestionsInfo[]?>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            _sentenceSuggestions = completion;

            var registration = cancellationToken.Register(() =>
            {
                WasCanceled = true;
                session.Cancel();
                completion.TrySetCanceled(cancellationToken);
            });

            completion.Task.ContinueWith(
                _ => registration.Dispose(),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);

            session.GetSentenceSuggestions([new TextServiceTextInfo(text)], suggestionsLimit);
            return completion.Task;
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
            var completion = new TaskCompletionSource<SuggestionsInfo[]?>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            _wordSuggestions = completion;

            var registration = cancellationToken.Register(() =>
            {
                WasCanceled = true;
                session.Cancel();
                completion.TrySetCanceled(cancellationToken);
            });

            completion.Task.ContinueWith(
                _ => registration.Dispose(),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);

#pragma warning disable CS0618 // Sentence queries can omit the word being edited.
            session.GetSuggestions(new TextServiceTextInfo(word), suggestionsLimit);
#pragma warning restore CS0618
            return completion.Task;
        }
    }
}
