using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Presenters;
using Avalonia.Input.TextInput;
using Avalonia.Logging;
using Avalonia.Threading;

namespace Avalonia.Controls;

internal sealed class TextBoxSpellCheckManager
{
    private static readonly TimeSpan CheckDelay = TimeSpan.FromMilliseconds(250);
    private const int MaxLayoutRetries = 3;
    private const int MaxProviderRetries = 3;

    private readonly TextBox _owner;
    private readonly SpellCheckResultCache _resultCache = new();
    private readonly SpellCheckHighlighter _highlighter = new();
    private readonly List<SpellCheckRange> _visibleRanges = new();
    private DispatcherTimer? _checkTimer;
    private TextPresenter? _presenter;
    private ScrollViewer? _scrollViewer;
    private CancellationTokenSource? _checkCancellation;
    private int _version;
    private int _layoutRetries;
    private int _providerRetries;
    private ISpellCheckProvider? _languageIdentityProvider;
    private string? _languageIdentity;

    // Share language-support results to avoid native calls on every keystroke.
    private static readonly ConditionalWeakTable<ISpellCheckProvider, Dictionary<string, bool>> s_languageSupport = new();

    public TextBoxSpellCheckManager(TextBox owner)
    {
        _owner = owner;
    }

    private bool TryGetContext(
        [NotNullWhen(true)] out string? text,
        [NotNullWhen(true)] out ISpellCheckProvider? provider,
        out CultureInfo? culture,
        bool refreshLanguageSupport = false)
    {
        if (!TryGetBaseContext(_owner, out text, out provider, out culture))
        {
            return false;
        }

        return IsLanguageSupported(provider, culture, refreshLanguageSupport);
    }

    // Language support can change, so a cached failure must not prevent the refresh timer from starting.
    private static bool TryGetBaseContext(
        TextBox owner,
        [NotNullWhen(true)] out string? text,
        [NotNullWhen(true)] out ISpellCheckProvider? provider,
        out CultureInfo? culture)
    {
        text = owner.Text;
        provider = null;
        culture = null;

        if (!HasCheckableText(text))
        {
            return false;
        }

        // Avoid reading the remaining options when spell checking is disabled.
        if (TextInputOptions.GetIsSpellCheckEnabled(owner) != true)
        {
            return false;
        }

        if (!TextInputOptions.IsSpellCheckAllowed(
                true,
                TextInputOptions.GetIsSensitive(owner),
                TextInputOptions.GetContentType(owner),
                owner.PasswordChar != default))
        {
            return false;
        }

        provider = TextInputOptions.GetSpellCheckProvider(owner) ?? TopLevel.GetTopLevel(owner)?.SpellCheckProvider;

        if (provider is null)
        {
            return false;
        }

        culture = ResolveCulture(TextInputOptions.GetLocaleHints(owner));
        return true;
    }

    internal static bool CanCreate(TextBox owner)
    {
        return TryGetBaseContext(owner, out _, out _, out _);
    }

    // Use the first valid hint, or let the provider choose the language.
    private static CultureInfo? ResolveCulture(IReadOnlyList<string>? hints)
    {
        if (hints is not { Count: > 0 })
        {
            return null;
        }

        for (var i = 0; i < hints.Count; i++)
        {
            var hint = hints[i];

            if (string.IsNullOrWhiteSpace(hint))
            {
                continue;
            }

            try
            {
#if NET6_0_OR_GREATER
                // Only real cultures: ICU would otherwise happily synthesise one for any tag.
                return CultureInfo.GetCultureInfo(hint, predefinedOnly: true);
#else
                return CultureInfo.GetCultureInfo(hint);
#endif
            }
            catch (CultureNotFoundException)
            {
                // Try the next hint.
            }
        }

        return null;
    }

    public void SetPresenter(TextPresenter? presenter, ScrollViewer? scrollViewer)
    {
        if (_presenter is not null && _presenter != presenter)
        {
            _highlighter.Clear(_presenter);
        }

        if (_scrollViewer is not null && _scrollViewer != scrollViewer)
        {
            _scrollViewer.ScrollChanged -= OnScrollChanged;
        }

        var presenterChanged = _presenter != presenter;
        _presenter = presenter;

        if (_scrollViewer != scrollViewer)
        {
            _scrollViewer = scrollViewer;

            if (_scrollViewer is not null)
            {
                _scrollViewer.ScrollChanged += OnScrollChanged;
            }
        }

        if (presenterChanged && presenter is not null)
        {
            // The new presenter may expose unchecked text.
            OnViewportChanged();
            return;
        }

        ApplyResults();
    }

    public bool ScheduleCheck(bool invalidateResults = false)
    {
        if (!TryGetBaseContext(_owner, out _, out _, out _))
        {
            Clear();
            return false;
        }

        if (invalidateResults)
        {
            InvalidateResults();
        }
        else
        {
            CancelPendingCheck();
        }

        RestartTimer();
        return true;
    }

    // Keep unaffected underlines. Only user edits suppress the word being typed.
    public bool OnTextChanged(string? oldText, string? newText, bool isUserEdit)
    {
        if (!TryGetBaseContext(_owner, out _, out _, out _))
        {
            Clear();
            return false;
        }

        // Any in-flight check reports offsets in the old text.
        CancelPendingCheck();

        if (newText is null || !_resultCache.TryApplyEdit(oldText, newText, isUserEdit && _owner.IsFocused))
        {
            _resultCache.Clear();
        }

        ApplyResults();
        RestartTimer();
        return true;
    }

    public void OnCaretMoved()
    {
        if (_resultCache.LastEditedWord is not { } edited)
        {
            return;
        }

        // Restore the underline when the caret leaves the edited word.
        var caret = _owner.CaretIndex;

        if (!_owner.IsFocused || caret <= edited.Start || caret > edited.End)
        {
            _resultCache.ClearLastEditedWord();
        }

        if (_resultCache.Results.Count > 0)
        {
            ApplyResults();
        }
    }

    private void RestartTimer()
    {
        _layoutRetries = 0;
        _providerRetries = 0;

        var timer = GetCheckTimer();
        timer.Stop();
        timer.Start();
    }

    public void Clear()
    {
        _checkTimer?.Stop();
        InvalidateResults();
    }

    private void InvalidateResults()
    {
        CancelPendingCheck();
        _resultCache.Clear();
        _languageIdentityProvider = null;
        _languageIdentity = null;
        _highlighter.Clear(_presenter);
    }

    private void CancelPendingCheck()
    {
        _version++;
        _checkCancellation?.Cancel();
        _checkCancellation = null;
    }

    public async ValueTask<(SpellCheckResult Result, IReadOnlyList<string> Suggestions)?> SuggestAsync(
        int caretIndex,
        int selectionStart,
        int selectionEnd,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!TryGetContext(out var text, out var provider, out var culture))
        {
            return null;
        }

        UpdateLanguageIdentity(provider, culture);

        var ranges = new List<SpellCheckRange>(1);
        SpellCheckRangeFinder.AddContextRange(ranges, text, caretIndex, selectionStart, selectionEnd);

        if (ranges.Count == 0)
        {
            return null;
        }

        if (!_resultCache.AreRangesChecked(text, ranges))
        {
            var results = await SpellChecker.CheckRangesAsync(
                text,
                ranges,
                provider,
                culture,
                cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            SetResults(text, ranges, results, merge: true);
        }

        SpellCheckResult result;

        if (!_resultCache.TryGetMisspelledWord(text, caretIndex, selectionStart, selectionEnd, out result) ||
            string.IsNullOrWhiteSpace(result.Word))
        {
            return null;
        }

        var suggestions = await provider.SuggestAsync(result.Word, culture, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        suggestions = SpellChecker.NormalizeSuggestions(result.Word, suggestions);

        if (suggestions.Count == 0)
        {
            return null;
        }

        return (result, suggestions);
    }

    private async void OnCheckTimerTick(object? sender, EventArgs e)
    {
        _checkTimer?.Stop();

        string? text;
        ISpellCheckProvider? provider;
        CultureInfo? culture;
        List<SpellCheckRange> visibleRanges;
        List<SpellCheckRange> uncheckedRanges;

        // Catch scheduling failures too: this is an async dispatcher callback.
        try
        {
            // Refresh once per debounce in case the keyboard language or dictionary changed.
            if (!TryGetContext(out text, out provider, out culture, refreshLanguageSupport: true))
            {
                Clear();
                return;
            }

            UpdateLanguageIdentity(provider, culture);

            if (_presenter is null)
            {
                // SetPresenter will schedule the check.
                return;
            }

            visibleRanges = new List<SpellCheckRange>();

            if (!SpellCheckRangeFinder.TryGetVisibleRanges(_presenter, _scrollViewer, text, visibleRanges))
            {
                // Layout may not be ready yet.
                if (_layoutRetries++ < MaxLayoutRetries)
                {
                    _checkTimer?.Start();
                }

                return;
            }

            uncheckedRanges = _resultCache.GetUncheckedRanges(text, visibleRanges);

            if (uncheckedRanges.Count == 0)
            {
                ApplyResults(visibleRanges);
                return;
            }
        }
        catch (Exception ex)
        {
            Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(
                _owner, "Spell check scheduling failed: {Error}", ex);
            return;
        }

        var version = ++_version;
        _checkCancellation?.Cancel();

        var cancellation = new CancellationTokenSource();
        _checkCancellation = cancellation;

        try
        {
            var results = await SpellChecker.CheckRangesAsync(
                text,
                uncheckedRanges,
                provider,
                culture,
                cancellation.Token);

            if (cancellation.IsCancellationRequested || version != _version)
            {
                return;
            }

            SetResults(text, uncheckedRanges, results, merge: true, visibleRanges: visibleRanges);
            _providerRetries = 0;
        }
        catch (OperationCanceledException)
        {
            // Expected when a newer spell-check request supersedes this one.
        }
        catch (Exception ex)
        {
            Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(
                _owner,
                "Spell check provider {Provider} failed: {Error}",
                provider.GetType().Name,
                ex);

            if (!cancellation.IsCancellationRequested && version == _version)
            {
                // Keep cached results and retry the failed range; a failure is not a clean check.
                if (_providerRetries++ < MaxProviderRetries)
                {
                    GetCheckTimer().Start();
                }
            }
        }
        finally
        {
            if (ReferenceEquals(_checkCancellation, cancellation))
            {
                _checkCancellation = null;
            }

            cancellation.Dispose();
        }
    }

    private DispatcherTimer GetCheckTimer()
    {
        if (_checkTimer is { } timer)
        {
            return timer;
        }

        timer = new DispatcherTimer { Interval = CheckDelay };
        timer.Tick += OnCheckTimerTick;
        _checkTimer = timer;

        return timer;
    }

    private static bool IsLanguageSupported(ISpellCheckProvider provider, CultureInfo? culture, bool refresh)
    {
        var cultureName = culture?.Name ?? string.Empty;
        var cache = s_languageSupport.GetValue(provider, static _ => new Dictionary<string, bool>(StringComparer.Ordinal));

        if (!refresh)
        {
            lock (cache)
            {
                if (cache.TryGetValue(cultureName, out var cached))
                {
                    return cached;
                }
            }
        }

        var supported = IsLanguageSupportedUncached(provider, culture);

        lock (cache)
        {
            cache[cultureName] = supported;
        }

        return supported;
    }

    private static bool IsLanguageSupportedUncached(ISpellCheckProvider provider, CultureInfo? culture)
    {
        try
        {
            return provider.IsLanguageSupported(culture);
        }
        catch (Exception ex)
        {
            Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(
                provider,
                "Spell check provider {Provider} failed to report language support: {Error}",
                provider.GetType().Name,
                ex);
            return false;
        }
    }

    private static bool HasCheckableText([NotNullWhen(true)] string? text)
    {
        return !string.IsNullOrEmpty(text);
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        OnViewportChanged();
    }

    public void OnViewportChanged()
    {
        _visibleRanges.Clear();
        var text = _owner.Text;

        if (_presenter is not null &&
            SpellCheckRangeFinder.TryGetVisibleRanges(_presenter, _scrollViewer, text, _visibleRanges) &&
            _resultCache.AreRangesChecked(text, _visibleRanges))
        {
            ApplyResults(_visibleRanges);
        }
        else
        {
            ScheduleCheck();
        }
    }

    private void SetResults(
        string text,
        List<SpellCheckRange> ranges,
        IReadOnlyList<SpellCheckResult> results,
        bool merge = false,
        List<SpellCheckRange>? visibleRanges = null)
    {
        _resultCache.Set(text, ranges, results, merge);
        ApplyResults(visibleRanges);
    }

    private void ApplyResults(List<SpellCheckRange>? visibleRanges = null)
    {
        if (_presenter is null || _resultCache.Results.Count == 0)
        {
            _highlighter.Clear(_presenter);
            return;
        }

        if (visibleRanges is null)
        {
            _visibleRanges.Clear();
            visibleRanges = _visibleRanges;
        }

        if (visibleRanges.Count == 0 &&
            !SpellCheckRangeFinder.TryGetVisibleRanges(_presenter, _scrollViewer, _owner.Text, visibleRanges))
        {
            _highlighter.Clear(_presenter);
            return;
        }

        // Hide only the word being edited, not an existing error the user clicked into.
        var exclude = _owner.IsFocused ? _resultCache.LastEditedWord : null;

        _highlighter.Apply(_presenter, _resultCache.Results, visibleRanges, exclude, _owner.CaretIndex);
    }

    private void UpdateLanguageIdentity(ISpellCheckProvider provider, CultureInfo? culture)
    {
        if (provider is not ISpellCheckProviderWithLanguageIdentity identityProvider ||
            identityProvider.GetLanguageIdentity(culture) is not { Length: > 0 } identity)
        {
            return;
        }

        if (ReferenceEquals(_languageIdentityProvider, provider) &&
            string.Equals(_languageIdentity, identity, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var changed = _languageIdentityProvider is not null;
        _languageIdentityProvider = provider;
        _languageIdentity = identity;

        if (changed)
        {
            // Cached results belong to the previous dictionary.
            _resultCache.Clear();
            _highlighter.Clear(_presenter);
        }
    }
}
