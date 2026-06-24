using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Presenters;
using Avalonia.Input.TextInput;
using Avalonia.Threading;

namespace Avalonia.Controls;

internal sealed class TextBoxSpellCheckManager
{
    private static readonly TimeSpan CheckDelay = TimeSpan.FromMilliseconds(250);

    private readonly TextBox _owner;
    private readonly SpellCheckResultCache _resultCache = new();
    private readonly SpellCheckHighlighter _highlighter = new();
    private DispatcherTimer? _checkTimer;
    private TextPresenter? _presenter;
    private ScrollViewer? _scrollViewer;
    private CancellationTokenSource? _checkCancellation;
    private int _version;

    public TextBoxSpellCheckManager(TextBox owner)
    {
        _owner = owner;
    }

    internal static bool CanCreate(TextBox owner)
    {
        var text = owner.Text;

        return CanCheck(owner) &&
            HasCheckableText(text) &&
            CanUseProvider(GetProvider(owner), CultureInfo.CurrentCulture);
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

        _presenter = presenter;

        if (_scrollViewer != scrollViewer)
        {
            _scrollViewer = scrollViewer;

            if (_scrollViewer is not null)
            {
                _scrollViewer.ScrollChanged += OnScrollChanged;
            }
        }

        ApplyResults();
    }

    public void ScheduleCheck(bool invalidateResults = false)
    {
        var text = _owner.Text;

        if (!CanCheck(_owner) ||
            !HasCheckableText(text) ||
            !CanUseProvider(GetProvider(_owner), CultureInfo.CurrentCulture))
        {
            Clear();
            return;
        }

        if (invalidateResults)
        {
            InvalidateResults();
        }
        else
        {
            CancelPendingCheck();
        }

        var timer = GetCheckTimer();
        timer.Stop();
        timer.Start();
    }

    public void RefreshStyles()
    {
        _highlighter.Refresh();
        ApplyResults();
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

        var text = _owner.Text;
        var provider = GetProvider(_owner);
        var culture = CultureInfo.CurrentCulture;

        if (provider is null ||
            !CanCheck(_owner) ||
            !HasCheckableText(text) ||
            !CanUseProvider(provider, culture))
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

        if (!CanCheck(_owner))
        {
            Clear();
            return;
        }

        var text = _owner.Text;
        var provider = GetProvider(_owner);

        if (provider is null || !HasCheckableText(text))
        {
            Clear();
            return;
        }

        var ranges = new List<SpellCheckRange>();

        if (_presenter is null ||
            !SpellCheckRangeFinder.TryGetVisibleRanges(_presenter, _scrollViewer, text, ranges))
        {
            Clear();
            return;
        }

        var version = ++_version;
        _checkCancellation?.Cancel();

        var cancellation = new CancellationTokenSource();
        _checkCancellation = cancellation;

        try
        {
            var culture = CultureInfo.CurrentCulture;

            if (!CanUseProvider(provider, culture))
            {
                Clear();
                return;
            }

            var results = await SpellChecker.CheckRangesAsync(
                text,
                ranges,
                provider,
                culture,
                cancellation.Token);

            if (cancellation.IsCancellationRequested || version != _version)
            {
                return;
            }

            SetResults(text, ranges, results);
        }
        catch (OperationCanceledException)
        {
            // Expected when a newer spell-check request supersedes this one.
        }
        catch (Exception)
        {
            if (!cancellation.IsCancellationRequested && version == _version)
            {
                Clear();
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

    private static bool CanCheck(TextBox owner)
    {
        var options = TextInputOptions.FromStyledElement(owner);
        return options.CanUseSpellCheck(owner.PasswordChar != default);
    }

    private static ISpellCheckProvider? GetProvider(TextBox owner)
    {
        return TextInputOptions.GetSpellCheckProvider(owner) ?? TopLevel.GetTopLevel(owner)?.SpellCheckProvider;
    }

    private static bool CanUseProvider(ISpellCheckProvider? provider, CultureInfo culture)
    {
        if (provider is null)
        {
            return false;
        }

        try
        {
            return provider.IsLanguageSupported(culture);
        }
        catch
        {
            return false;
        }
    }

    private static bool HasCheckableText([NotNullWhen(true)] string? text)
    {
        return !string.IsNullOrEmpty(text);
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        var ranges = new List<SpellCheckRange>();
        var text = _owner.Text;

        if (_presenter is not null &&
            SpellCheckRangeFinder.TryGetVisibleRanges(_presenter, _scrollViewer, text, ranges) &&
            _resultCache.AreRangesChecked(text, ranges))
        {
            ApplyResults();
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
        bool merge = false)
    {
        _resultCache.Set(text, ranges, results, merge);
        ApplyResults();
    }

    private void ApplyResults()
    {
        if (_presenter is null || _resultCache.Results.Count == 0 || !CanCheck(_owner))
        {
            _highlighter.Clear(_presenter);
            return;
        }

        var ranges = new List<SpellCheckRange>();

        if (!SpellCheckRangeFinder.TryGetVisibleRanges(_presenter, _scrollViewer, _owner.Text, ranges))
        {
            _highlighter.Clear(_presenter);
            return;
        }

        _highlighter.Apply(_presenter, _resultCache.Results, ranges);
    }
}
