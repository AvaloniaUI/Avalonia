using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Presenters;
using Avalonia.Input.TextInput;
using Avalonia.Logging;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Avalonia.Controls;

internal sealed class TextBoxSpellCheckManager : IDisposable
{
    private static readonly TimeSpan CheckDelay = TimeSpan.FromMilliseconds(250);
    private static readonly HashSet<string> s_warnings = new(StringComparer.OrdinalIgnoreCase);
    private const int MaxLayoutRetries = 3;
    private const int MaxProviderRetries = 3;

    private readonly TextBox _owner;
    private readonly SpellCheckResultCache _resultCache = new();
    private readonly SpellCheckHighlighter _highlighter = new();
    private readonly List<SpellCheckRange> _visibleRanges = new();
    private readonly IPlatformSettings? _platformSettings;
    private DispatcherTimer? _checkTimer;
    private TextPresenter? _presenter;
    private ScrollViewer? _scrollViewer;
    private CancellationTokenSource? _checkCancellation;
    private int _version;
    private int _layoutRetries;
    private int _providerRetries;
    private ISpellCheckProvider? _contextProvider;
    private CultureInfo? _contextCulture;
    private SpellCheckContextGate? _context;

    public TextBoxSpellCheckManager(TextBox owner)
    {
        _owner = owner;
        _platformSettings = Application.Current?.PlatformSettings;

        if (_platformSettings is not null)
        {
            _platformSettings.PreferredApplicationLanguageChanged += OnPreferredApplicationLanguageChanged;
        }
    }

    public void Dispose()
    {
        if (_platformSettings is not null)
        {
            _platformSettings.PreferredApplicationLanguageChanged -= OnPreferredApplicationLanguageChanged;
        }

        Clear();
        SetPresenter(null, null);
    }

    private void OnPreferredApplicationLanguageChanged(object? sender, EventArgs e)
    {
        if (SpellCheck.GetLanguage(_owner) is null && ResolveLocaleHintCulture(_owner) is null)
        {
            ScheduleCheck(invalidateResults: true);
        }
    }

    private bool TryGetContext(
        [NotNullWhen(true)] out string? text,
        [NotNullWhen(true)] out ISpellCheckProvider? provider,
        [NotNullWhen(true)] out SpellCheckContextGate? context)
    {
        context = null;

        if (!TryGetBaseContext(_owner, out text, out provider, out var culture))
        {
            return false;
        }

        context = GetOrCreateContext(provider, culture);
        return context is not null;
    }

    // Language support can change, so a cached failure must not prevent the refresh timer from starting.
    private static bool TryGetBaseContext(
        TextBox owner,
        [NotNullWhen(true)] out string? text,
        [NotNullWhen(true)] out ISpellCheckProvider? provider,
        [NotNullWhen(true)] out CultureInfo? culture)
    {
        text = owner.Text;
        provider = null;
        culture = null;

        if (!HasCheckableText(text))
        {
            return false;
        }

        // Avoid reading the remaining options when spell checking is disabled.
        if (!SpellCheck.GetIsEnabled(owner))
        {
            return false;
        }

        if (!IsSpellCheckAllowed(owner))
        {
            return false;
        }

        provider = SpellCheck.GetProvider(owner)
            ?? AvaloniaLocator.Current.GetService<ISpellCheckProvider>()
            ?? TopLevel.GetTopLevel(owner)?.PlatformImpl?.TryGetFeature<ISpellCheckProvider>();

        if (provider is null)
        {
            return false;
        }

        if (SpellCheck.GetLanguage(owner) is { Length: > 0 } explicitTag)
        {
            // An explicit language that cannot be resolved must not fall back to checking in another language.
            culture = TryGetCulture(explicitTag);

            if (culture is null)
            {
                if (ShouldWarn(explicitTag))
                {
                    Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(
                        null, "Spell check language '{Language}' cannot be resolved, so spell checking is off.", explicitTag);
                }
                return false;
            }
        }
        else
        {
            culture = ResolveLocaleHintCulture(owner) ?? ResolveSystemCulture();
        }

        return true;
    }

    // Each problem is logged once per process, because this runs on every edit.
    private static bool ShouldWarn(string key) => s_warnings.Add(key);

    internal static bool CanCreate(TextBox owner)
    {
        return TryGetBaseContext(owner, out _, out _, out _);
    }

    private static bool IsSpellCheckAllowed(TextBox owner)
    {
        return SpellCheckPolicy.IsAllowed(
            TextInputOptions.GetContentType(owner),
            TextInputOptions.GetIsSensitive(owner),
            owner.PasswordChar != default);
    }

    private static CultureInfo? ResolveLocaleHintCulture(TextBox owner)
    {
        if (TextInputOptions.GetLocaleHints(owner) is not { Count: > 0 } hints)
        {
            return null;
        }

        foreach (var hint in hints)
        {
            if (TryGetCulture(hint) is { } culture)
            {
                return culture;
            }
        }

        return null;
    }

    private static CultureInfo? TryGetCulture(string? languageTag)
    {
        if (string.IsNullOrWhiteSpace(languageTag))
        {
            return null;
        }

        try
        {
            return CultureInfo.GetCultureInfo(languageTag);
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }

    private static CultureInfo ResolveSystemCulture()
    {
        return TryGetCulture(Application.Current?.PlatformSettings?.PreferredApplicationLanguage)
            ?? CultureInfo.CurrentUICulture;
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
        DisposeContext();
        _highlighter.Clear(_presenter);
    }

    private void CancelPendingCheck()
    {
        _version++;
        _checkCancellation?.Cancel();
        _checkCancellation = null;
    }

    public async ValueTask<(ISpellCheckResult Result, IReadOnlyList<string> Suggestions)?> SuggestAsync(
        int caretIndex,
        int selectionStart,
        int selectionEnd,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!TryGetContext(out var text, out _, out var context))
        {
            return null;
        }

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
                context,
                cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            SetResults(text, ranges, results, merge: true);
        }

        ISpellCheckResult result;

        if (!_resultCache.TryGetMisspelledWord(text, caretIndex, selectionStart, selectionEnd, out result))
        {
            return null;
        }

        var word = text.Substring(result.Start, result.Length);
        var suggestions = await context.SuggestAsync(result, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        suggestions = SpellChecker.NormalizeSuggestions(word, suggestions);

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
        SpellCheckContextGate? context;
        List<SpellCheckRange> visibleRanges;
        List<SpellCheckRange> uncheckedRanges;

        // Catch scheduling failures too: this is an async dispatcher callback.
        try
        {
            if (!TryGetContext(out text, out provider, out context))
            {
                Clear();
                return;
            }

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
                context,
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
        catch (ObjectDisposedException) when (!ReferenceEquals(_context, context))
        {
            // The context was replaced while this check was queued.
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
        IReadOnlyList<ISpellCheckResult> results,
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

    private SpellCheckContextGate? GetOrCreateContext(ISpellCheckProvider provider, CultureInfo culture)
    {
        if (_context is not null &&
            ReferenceEquals(_contextProvider, provider) &&
            Equals(_contextCulture, culture))
        {
            return _context;
        }

        DisposeContext();
        _resultCache.Clear();
        _highlighter.Clear(_presenter);

        try
        {
            if (provider.CreateContext(culture) is not { } context)
            {
                // The invariant culture usually means no system language is set, for example LANG=C on Linux.
                if (ShouldWarn(provider.GetType().FullName + "|" + culture.Name))
                {
                    Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(
                        provider,
                        "Spell check provider {Provider} has no dictionary for {Culture}. Set SpellCheck.Language to choose one.",
                        provider.GetType().Name,
                        culture.Name.Length == 0 ? "the invariant culture" : culture.Name);
                }
                return null;
            }

            _context = new SpellCheckContextGate(context);
            _contextProvider = provider;
            _contextCulture = culture;
            return _context;
        }
        catch (Exception ex)
        {
            Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(
                provider,
                "Spell check provider {Provider} failed to create a context for {Culture}: {Error}",
                provider.GetType().Name,
                culture.Name,
                ex);
            return null;
        }
    }

    private void DisposeContext()
    {
        if (_context is { } context)
        {
            _context = null;
            _contextProvider = null;
            _contextCulture = null;
            context.Dispose();
        }
    }
}
