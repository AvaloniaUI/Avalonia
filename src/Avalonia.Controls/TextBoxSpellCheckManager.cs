using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
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
    // Languages are resolved on every edit, so the resolved lists are cached.
    private static readonly ConditionalWeakTable<IReadOnlyList<string>, CultureInfo[]?> s_languageLists = new();
    private static readonly Dictionary<CultureInfo, CultureInfo[]> s_singleCultures = new();
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
    private IReadOnlyList<CultureInfo>? _contextCultures;
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
        if (GetExplicitCultures(_owner) is null && ResolveLocaleHintCulture(_owner) is null)
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

        if (!TryGetBaseContext(_owner, out text, out provider, out var cultures))
        {
            return false;
        }

        context = GetOrCreateContext(provider, cultures);
        return context is not null;
    }

    // Language support can change, so a cached failure must not prevent the refresh timer from starting.
    private static bool TryGetBaseContext(
        TextBox owner,
        [NotNullWhen(true)] out string? text,
        [NotNullWhen(true)] out ISpellCheckProvider? provider,
        [NotNullWhen(true)] out IReadOnlyList<CultureInfo>? cultures)
    {
        text = owner.Text;
        provider = null;
        cultures = null;

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

        if (GetExplicitCultures(owner) is { } explicitCultures)
        {
            // Explicit languages that cannot be resolved must not fall back to checking in another language.
            cultures = explicitCultures;
            return explicitCultures.Length > 0;
        }

        var culture = ResolveLocaleHintCulture(owner) ?? ResolveSystemCulture();

        if (!s_singleCultures.TryGetValue(culture, out var single))
        {
            single = new[] { culture };
            s_singleCultures[culture] = single;
        }

        cultures = single;
        return true;
    }

    // Null when SpellCheck.Languages is unset or has no tags; empty when no tag resolves.
    private static CultureInfo[]? GetExplicitCultures(TextBox owner)
    {
        if (SpellCheck.GetLanguages(owner) is not { Count: > 0 } tags)
        {
            return null;
        }

        if (!s_languageLists.TryGetValue(tags, out var cultures))
        {
            cultures = ResolveExplicitCultures(tags);
            s_languageLists.AddOrUpdate(tags, cultures);
        }

        return cultures;
    }

    private static CultureInfo[]? ResolveExplicitCultures(IReadOnlyList<string> tags)
    {
        var hasTags = false;
        List<CultureInfo>? cultures = null;

        for (var i = 0; i < tags.Count; i++)
        {
            var tag = tags[i];

            if (string.IsNullOrWhiteSpace(tag))
            {
                continue;
            }

            hasTags = true;

            if (TryGetCulture(tag) is { } culture)
            {
                cultures ??= new List<CultureInfo>();

                if (!cultures.Contains(culture))
                {
                    cultures.Add(culture);
                }
            }
            else if (ShouldWarn(tag))
            {
                Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(
                    null, "Spell check language '{Language}' cannot be resolved and is skipped.", tag);
            }
        }

        if (!hasTags)
        {
            return null;
        }

        return cultures?.ToArray() ?? Array.Empty<CultureInfo>();
    }

    // Each problem is logged once per process, because this runs on every edit.
    private static bool ShouldWarn(string key) => s_warnings.Add(key);

    internal static bool CanCreate(TextBox owner)
    {
        return TryGetBaseContext(owner, out _, out _, out _);
    }

    private static bool IsSpellCheckAllowed(TextBox owner)
    {
        // A mask describes structured input such as phone numbers or codes.
        return owner is not MaskedTextBox { Mask.Length: > 0 } &&
            SpellCheckPolicy.IsAllowed(
            TextInputOptions.GetContentType(owner),
            TextInputOptions.GetIsSensitive(owner),
            owner.PasswordChar != default);
    }

    // Only the first hint: one language that accepts every word would turn all underlines off.
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

        if (_owner.MaxLength > 0)
        {
            // TextBox truncates input at MaxLength, which would corrupt a longer replacement.
            var remaining = text.Length - result.Length;
            suggestions = suggestions.Where(s => remaining + s.Length <= _owner.MaxLength).ToArray();
        }

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

            // The viewport may have changed while the check ran, so apply to the current one.
            SetResults(text, uncheckedRanges, results, merge: true);
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

    private SpellCheckContextGate? GetOrCreateContext(ISpellCheckProvider provider, IReadOnlyList<CultureInfo> cultures)
    {
        if (_context is not null &&
            ReferenceEquals(_contextProvider, provider) &&
            _contextCultures is not null &&
            (ReferenceEquals(_contextCultures, cultures) || _contextCultures.SequenceEqual(cultures)))
        {
            return _context;
        }

        DisposeContext();
        _resultCache.Clear();
        _highlighter.Clear(_presenter);

        var contexts = new List<ISpellCheckContext>(cultures.Count);

        foreach (var culture in cultures)
        {
            if (CreateContext(provider, culture) is { } context)
            {
                contexts.Add(context);
            }
        }

        if (contexts.Count == 0)
        {
            return null;
        }

        _context = new SpellCheckContextGate(contexts.Count == 1 ? contexts[0] : new CompositeSpellCheckContext(contexts));
        _contextProvider = provider;
        _contextCultures = cultures;
        return _context;
    }

    private static ISpellCheckContext? CreateContext(ISpellCheckProvider provider, CultureInfo culture)
    {
        try
        {
            if (provider.CreateContext(culture) is { } context)
            {
                return context;
            }

            // The invariant culture usually means no system language is set, for example LANG=C on Linux.
            if (ShouldWarn(provider.GetType().FullName + "|" + culture.Name))
            {
                Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(
                    provider,
                    "Spell check provider {Provider} has no dictionary for {Culture}. Set SpellCheck.Languages to choose one.",
                    provider.GetType().Name,
                    culture.Name.Length == 0 ? "the invariant culture" : culture.Name);
            }

            return null;
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
            _contextCultures = null;
            context.Dispose();
        }
    }
}
