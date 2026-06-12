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

    public async ValueTask<IReadOnlyList<SpellCheckResult>> CheckAsync(
        string text,
        CultureInfo? culture,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_manager is not { } manager ||
            string.IsNullOrEmpty(text))
        {
            return Array.Empty<SpellCheckResult>();
        }

        using var listener = new SessionListener();
        var session = CreateSession(manager, culture, listener);

        if (session is null)
        {
            return Array.Empty<SpellCheckResult>();
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(QueryTimeout);

            var results = await listener.GetSentenceSuggestionsAsync(session, text, MaxSuggestions, timeout.Token)
                .ConfigureAwait(false);

            return GetMisspellings(text, results);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Array.Empty<SpellCheckResult>();
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

        using var listener = new SessionListener();
        var session = CreateSession(manager, culture, listener);

        if (session is null)
        {
            return Array.Empty<string>();
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(QueryTimeout);

            var results = await listener.GetSentenceSuggestionsAsync(session, word, MaxSuggestions, timeout.Token)
                .ConfigureAwait(false);

            return GetSuggestions(results);
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
        var locale = CreateLocale(culture);

        return manager.NewSpellCheckerSession(null, locale, listener, true);
    }

    private static Locale? CreateLocale(CultureInfo? culture)
    {
        var languageTag = culture?.Name;

        if (string.IsNullOrEmpty(languageTag))
        {
            languageTag = CultureInfo.CurrentCulture.Name;
        }

        return string.IsNullOrEmpty(languageTag) ? null : Locale.ForLanguageTag(languageTag);
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
                misspellings.Add(new SpellCheckResult(start, length, text.Substring(start, length)));
            }
        }

        return misspellings.Count == 0 ? Array.Empty<SpellCheckResult>() : misspellings;
    }

    private static IReadOnlyList<string> GetSuggestions(SentenceSuggestionsInfo[]? results)
    {
        if (results is null || results.Length == 0)
        {
            return Array.Empty<string>();
        }

        foreach (var sentence in results)
        {
            if (sentence is null)
            {
                continue;
            }

            for (var i = 0; i < sentence.SuggestionsCount; i++)
            {
                var suggestionInfo = sentence.GetSuggestionsInfoAt(i);

                if (suggestionInfo is null || suggestionInfo.SuggestionsCount <= 0)
                {
                    continue;
                }

                var suggestions = new List<string>(suggestionInfo.SuggestionsCount);

                for (var j = 0; j < suggestionInfo.SuggestionsCount; j++)
                {
                    var suggestion = suggestionInfo.GetSuggestionAt(j);

                    if (!string.IsNullOrEmpty(suggestion))
                    {
                        suggestions.Add(suggestion);
                    }
                }

                return suggestions.Count == 0 ? Array.Empty<string>() : suggestions;
            }
        }

        return Array.Empty<string>();
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
                // Ignore malformed culture tags and continue without a neutral fallback.
            }
        }

        if (!string.IsNullOrEmpty(neutralLanguageTag))
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

        return string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(expected.Replace('-', '_'), actual, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(expected.Replace('_', '-'), actual, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class SessionListener : Object, SpellCheckerSession.ISpellCheckerSessionListener
    {
        private TaskCompletionSource<SentenceSuggestionsInfo[]?>? _sentenceSuggestions;

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
        }
    }
}
