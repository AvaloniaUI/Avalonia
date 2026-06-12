using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Utils;
using Avalonia.Input.TextInput;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;
using Avalonia.Utilities;
using Avalonia.VisualTree;

namespace Avalonia.Controls;

internal sealed class TextBoxSpellCheckController
{
    private static readonly TimeSpan CheckDelay = TimeSpan.FromMilliseconds(250);
    private const int MaxSuggestionCount = 8;

    private readonly TextBox _owner;
    private DispatcherTimer? _checkTimer;
    private TextPresenter? _presenter;
    private ScrollViewer? _scrollViewer;
    private CancellationTokenSource? _checkCancellation;
    private IReadOnlyList<SpellCheckResult> _results = Array.Empty<SpellCheckResult>();
    private IReadOnlyList<TextRange> _checkedRanges = Array.Empty<TextRange>();
    private TextRunProperties? _misspellingTextRunProperties;
    private string? _checkedText;
    private int _version;

    public TextBoxSpellCheckController(TextBox owner)
    {
        _owner = owner;
    }

    internal static bool CanCreate(TextBox owner)
    {
        var text = owner.Text;

        return CanCheck(owner) &&
            CanCheckText(text) &&
            CanUseProvider(GetProvider(owner), CultureInfo.CurrentCulture);
    }

    public void SetPresenter(TextPresenter? presenter, ScrollViewer? scrollViewer)
    {
        if (_presenter is not null && _presenter != presenter)
        {
            _presenter.SetTextStyleOverrides(null);
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
            !CanCheckText(text) ||
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
        _misspellingTextRunProperties = null;
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
        _results = Array.Empty<SpellCheckResult>();
        _checkedRanges = Array.Empty<TextRange>();
        _misspellingTextRunProperties = null;
        _checkedText = null;
        _presenter?.SetTextStyleOverrides(null);
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
            !CanCheckText(text) ||
            !CanUseProvider(provider, culture))
        {
            return null;
        }

        var ranges = new List<TextRange>(1);
        AddContextTextRange(ranges, text, caretIndex, selectionStart, selectionEnd);

        if (ranges.Count == 0)
        {
            return null;
        }

        if (!AreRangesChecked(text, ranges))
        {
            var results = await CheckRangesAsync(text, ranges, provider, culture, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            SetResults(text, ranges, results, merge: true);
        }

        SpellCheckResult result;

        if (!TryGetMisspelledWord(caretIndex, selectionStart, selectionEnd, out result) ||
            string.IsNullOrWhiteSpace(result.Word))
        {
            return null;
        }

        var suggestions = await provider.SuggestAsync(result.Word, culture, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        suggestions = NormalizeSuggestions(result.Word, suggestions);

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

        if (provider is null || !CanCheckText(text))
        {
            Clear();
            return;
        }

        var ranges = new List<TextRange>();

        if (!TryGetVisibleTextRanges(ranges))
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

            var results = await CheckRangesAsync(text, ranges, provider, culture, cancellation.Token);

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

    internal static bool CanCheckText([NotNullWhen(true)] string? text)
    {
        return !string.IsNullOrEmpty(text);
    }

    private bool TryGetMisspelledWord(
        int caretIndex,
        int selectionStart,
        int selectionEnd,
        out SpellCheckResult result)
    {
        result = default;

        if (_results.Count == 0 || !CanCheck(_owner) || string.IsNullOrEmpty(_owner.Text))
        {
            return false;
        }

        var selectedStart = Math.Min(selectionStart, selectionEnd);
        var selectedEnd = Math.Max(selectionStart, selectionEnd);
        var hasSelection = selectedStart != selectedEnd;

        foreach (var candidate in _results)
        {
            var candidateStart = candidate.Start;
            var candidateEnd = candidate.Start + candidate.Length;
            var isMatch = hasSelection
                ? candidateStart < selectedEnd && candidateEnd > selectedStart
                : caretIndex >= candidateStart && caretIndex <= candidateEnd;

            if (!isMatch)
            {
                continue;
            }

            result = candidate.Word is not null
                ? candidate
                : candidate with
                {
                    Word = candidateStart >= 0 && candidateEnd <= _owner.Text.Length
                        ? _owner.Text.Substring(candidateStart, candidate.Length)
                        : null
                };

            return true;
        }

        return false;
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        var ranges = new List<TextRange>();

        if (TryGetVisibleTextRanges(ranges) && AreRangesChecked(_owner.Text, ranges))
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
        List<TextRange> ranges,
        IReadOnlyList<SpellCheckResult> results,
        bool merge = false)
    {
        if (merge && string.Equals(_checkedText, text, StringComparison.Ordinal))
        {
            _results = MergeResults(_results, ranges, results);
            _checkedRanges = MergeCheckedRanges(_checkedRanges, ranges);
        }
        else
        {
            _results = results;
            _checkedRanges = ranges.Count == 0 ? Array.Empty<TextRange>() : ranges.ToArray();
        }

        _checkedText = text;
        ApplyResults();
    }

    private static IReadOnlyList<SpellCheckResult> MergeResults(
        IReadOnlyList<SpellCheckResult> existing,
        List<TextRange> ranges,
        IReadOnlyList<SpellCheckResult> results)
    {
        if (existing.Count == 0)
        {
            if (results.Count == 0)
            {
                return Array.Empty<SpellCheckResult>();
            }

            var sorted = new List<SpellCheckResult>(results);
            SortResults(sorted);
            return sorted;
        }

        var merged = new List<SpellCheckResult>(existing.Count + results.Count);

        for (var i = 0; i < existing.Count; i++)
        {
            if (!IntersectsAnyRange(existing[i], ranges))
            {
                merged.Add(existing[i]);
            }
        }

        for (var i = 0; i < results.Count; i++)
        {
            merged.Add(results[i]);
        }

        if (merged.Count == 0)
        {
            return Array.Empty<SpellCheckResult>();
        }

        SortResults(merged);
        return merged;
    }

    private static void SortResults(List<SpellCheckResult> results)
    {
        results.Sort(static (x, y) =>
        {
            var start = x.Start.CompareTo(y.Start);
            return start != 0 ? start : x.Length.CompareTo(y.Length);
        });
    }

    private static IReadOnlyList<TextRange> MergeCheckedRanges(
        IReadOnlyList<TextRange> existing,
        List<TextRange> ranges)
    {
        if (existing.Count == 0)
        {
            return ranges.Count == 0 ? Array.Empty<TextRange>() : ranges.ToArray();
        }

        if (ranges.Count == 0)
        {
            return existing;
        }

        var merged = new List<TextRange>(existing.Count + ranges.Count);

        for (var i = 0; i < existing.Count; i++)
        {
            merged.Add(existing[i]);
        }

        for (var i = 0; i < ranges.Count; i++)
        {
            merged.Add(ranges[i]);
        }

        merged.Sort(static (x, y) =>
        {
            var start = x.Start.CompareTo(y.Start);
            return start != 0 ? start : x.End.CompareTo(y.End);
        });

        var writeIndex = 0;

        for (var readIndex = 1; readIndex < merged.Count; readIndex++)
        {
            var current = merged[readIndex];
            var last = merged[writeIndex];

            if (current.Start <= last.End)
            {
                if (current.End > last.End)
                {
                    merged[writeIndex] = new TextRange(last.Start, current.End);
                }
            }
            else
            {
                writeIndex++;
                merged[writeIndex] = current;
            }
        }

        if (writeIndex + 1 < merged.Count)
        {
            merged.RemoveRange(writeIndex + 1, merged.Count - writeIndex - 1);
        }

        return merged;
    }

    private bool AreRangesChecked(string? text, List<TextRange> ranges)
    {
        if (ranges.Count == 0 ||
            _checkedRanges.Count == 0 ||
            !string.Equals(_checkedText, text, StringComparison.Ordinal))
        {
            return false;
        }

        for (var i = 0; i < ranges.Count; i++)
        {
            if (!IsRangeChecked(ranges[i]))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsRangeChecked(TextRange range)
    {
        for (var i = 0; i < _checkedRanges.Count; i++)
        {
            var checkedRange = _checkedRanges[i];

            if (range.Start >= checkedRange.Start && range.End <= checkedRange.End)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IntersectsAnyRange(SpellCheckResult result, List<TextRange> ranges)
    {
        var resultEnd = result.Start + result.Length;

        for (var i = 0; i < ranges.Count; i++)
        {
            var range = ranges[i];

            if (resultEnd > range.Start && result.Start < range.End)
            {
                return true;
            }
        }

        return false;
    }

    private void ApplyResults()
    {
        if (_presenter is null || _results.Count == 0 || !CanCheck(_owner))
        {
            _presenter?.SetTextStyleOverrides(null);
            return;
        }

        var ranges = new List<TextRange>();

        if (!TryGetVisibleTextRanges(ranges))
        {
            _presenter.SetTextStyleOverrides(null);
            return;
        }

        List<ValueSpan<TextRunProperties>>? spans = null;

        for (var i = 0; i < _results.Count; i++)
        {
            var result = _results[i];

            if (!IntersectsVisibleRange(result, ranges))
            {
                continue;
            }

            _misspellingTextRunProperties ??= CreateMisspellingTextRunProperties(_presenter);
            spans ??= new List<ValueSpan<TextRunProperties>>();
            spans.Add(new ValueSpan<TextRunProperties>(result.Start, result.Length, _misspellingTextRunProperties));
        }

        _presenter.SetTextStyleOverrides(spans);
    }

    private static TextRunProperties CreateMisspellingTextRunProperties(TextPresenter presenter)
    {
        var typeface = new Typeface(
            presenter.FontFamily,
            presenter.FontStyle,
            presenter.FontWeight,
            presenter.FontStretch);

        var decorations = new TextDecorationCollection
        {
            new TextDecoration
            {
                Location = TextDecorationLocation.Underline,
                Stroke = Brushes.Red,
                StrokeDashArray = new AvaloniaList<double> { 1, 2 },
                StrokeLineCap = PenLineCap.Round
            }
        };

        return new GenericTextRunProperties(
            typeface,
            presenter.FontSize,
            decorations,
            presenter.Foreground,
            fontFeatures: presenter.FontFeatures);
    }

    private bool TryGetVisibleTextRanges(List<TextRange> ranges)
    {
        if (_presenter is null)
        {
            return false;
        }

        var text = _owner.Text;

        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        var textLength = text.Length;

        var textLines = _presenter.TextLayout.TextLines;

        if (textLines.Count == 0)
        {
            return false;
        }

        var viewport = GetViewportInPresenter();
        var currentY = 0.0;

        for (var i = 0; i < textLines.Count; i++)
        {
            var textLine = textLines[i];
            var lineTop = currentY;
            var lineBottom = currentY + textLine.Height;
            currentY = lineBottom;

            if (lineBottom < viewport.Top)
            {
                continue;
            }

            if (lineTop > viewport.Bottom)
            {
                break;
            }

            AddVisibleTextRange(ranges, textLine, viewport.Left, viewport.Right, text);
        }

        return ranges.Count > 0;
    }

    private Rect GetViewportInPresenter()
    {
        if (_presenter is null)
        {
            return default;
        }

        if (_scrollViewer is not null &&
            _scrollViewer.Viewport.Width > 0 &&
            _scrollViewer.Viewport.Height > 0 &&
            _scrollViewer.TranslatePoint(default, _presenter) is { } topLeft &&
            _scrollViewer.TranslatePoint(new Point(_scrollViewer.Viewport.Width, _scrollViewer.Viewport.Height), _presenter) is { } bottomRight)
        {
            var x = Math.Min(topLeft.X, bottomRight.X);
            var y = Math.Min(topLeft.Y, bottomRight.Y);

            return new Rect(x, y, Math.Abs(bottomRight.X - topLeft.X), Math.Abs(bottomRight.Y - topLeft.Y));
        }

        return new Rect(_presenter.Bounds.Size);
    }

    private static void AddVisibleTextRange(
        List<TextRange> ranges,
        TextLine textLine,
        double viewportLeft,
        double viewportRight,
        string text)
    {
        var textLength = text.Length;
        var lineStart = textLine.FirstTextSourceIndex;
        var lineEnd = Math.Min(textLength, textLine.FirstTextSourceIndex + textLine.Length);
        var startIsInsideWord = false;
        var endIsInsideWord = false;

        if (lineEnd <= lineStart)
        {
            return;
        }

        if (viewportLeft > 0 || viewportRight < textLine.WidthIncludingTrailingWhitespace)
        {
            var start = GetTextPosition(textLine.GetCharacterHitFromDistance(viewportLeft));
            var end = GetTextPosition(textLine.GetCharacterHitFromDistance(viewportRight));

            if (start > end)
            {
                (start, end) = (end, start);
            }

            startIsInsideWord = IsInsideWord(text, start);
            endIsInsideWord = IsInsideWord(text, end);
            lineStart = Math.Max(lineStart, start - 1);
            lineEnd = Math.Min(lineEnd, end + 1);

            if (lineEnd <= lineStart)
            {
                return;
            }
        }

        if (ranges.Count > 0)
        {
            var last = ranges[ranges.Count - 1];

            if (lineStart <= last.End)
            {
                if (lineEnd > last.End)
                {
                    ranges[ranges.Count - 1] = new TextRange(
                        last.Start,
                        lineEnd,
                        last.StartIsInsideWord,
                        endIsInsideWord);
                }

                return;
            }
        }

        ranges.Add(new TextRange(lineStart, lineEnd, startIsInsideWord, endIsInsideWord));
    }

    private static bool IsInsideWord(string text, int index)
    {
        return index > 0 &&
            index < text.Length &&
            char.IsLetterOrDigit(text[index - 1]) &&
            char.IsLetterOrDigit(text[index]);
    }

    private static void AddContextTextRange(
        List<TextRange> ranges,
        string text,
        int caretIndex,
        int selectionStart,
        int selectionEnd)
    {
        var textLength = text.Length;
        var rangeStart = MathUtilities.Clamp(Math.Min(selectionStart, selectionEnd), 0, textLength);
        var rangeEnd = MathUtilities.Clamp(Math.Max(selectionStart, selectionEnd), 0, textLength);

        if (rangeStart == rangeEnd)
        {
            var caret = MathUtilities.Clamp(caretIndex, 0, textLength);

            rangeStart = StringUtils.PreviousWord(text, caret);
            rangeEnd = StringUtils.NextWord(text, rangeStart);
        }
        else
        {
            rangeStart = StringUtils.PreviousWord(text, rangeStart);
            rangeEnd = StringUtils.NextWord(text, rangeEnd);
        }

        rangeStart = MathUtilities.Clamp(rangeStart, 0, textLength);
        rangeEnd = MathUtilities.Clamp(rangeEnd, rangeStart, textLength);

        if (rangeEnd > rangeStart)
        {
            ranges.Add(new TextRange(rangeStart, rangeEnd));
        }
    }

    private static int GetTextPosition(CharacterHit characterHit)
    {
        return characterHit.FirstCharacterIndex + characterHit.TrailingLength;
    }

    private static async ValueTask<IReadOnlyList<SpellCheckResult>> CheckRangesAsync(
        string text,
        List<TextRange> ranges,
        ISpellCheckProvider provider,
        CultureInfo culture,
        CancellationToken cancellationToken)
    {
        List<SpellCheckResult>? normalized = null;

        for (var i = 0; i < ranges.Count; i++)
        {
            var range = ranges[i];
            var length = range.End - range.Start;

            if (length <= 0 || IsWhiteSpace(text, range.Start, length))
            {
                continue;
            }

            var rangeText = text.Substring(range.Start, length);
            var results = await provider.CheckAsync(rangeText, culture, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            if (results.Count == 0)
            {
                continue;
            }

            AddNormalizedResults(results, range, length, normalized ??= new List<SpellCheckResult>(results.Count));
        }

        if (normalized is null || normalized.Count == 0)
        {
            return Array.Empty<SpellCheckResult>();
        }

        SortResults(normalized);
        return normalized;
    }

    private static bool IsWhiteSpace(string text, int start, int length)
    {
        var end = Math.Min(text.Length, start + length);

        for (var i = start; i < end; i++)
        {
            if (!char.IsWhiteSpace(text[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IntersectsVisibleRange(SpellCheckResult result, List<TextRange> ranges)
    {
        var resultEnd = result.Start + result.Length;

        for (var i = 0; i < ranges.Count; i++)
        {
            var range = ranges[i];

            if (resultEnd <= range.Start)
            {
                continue;
            }

            if (result.Start < range.End)
            {
                return true;
            }
        }

        return false;
    }

    private static void AddNormalizedResults(
        IReadOnlyList<SpellCheckResult> results,
        TextRange range,
        int textLength,
        List<SpellCheckResult> normalized)
    {
        foreach (var result in results)
        {
            if (result.Start < 0 || result.Length <= 0 || result.Start >= textLength)
            {
                continue;
            }

            var length = Math.Min(result.Length, textLength - result.Start);

            // Avoid underlining a partial word produced by a horizontally clipped visible range.
            if ((range.StartIsInsideWord && result.Start == 0) ||
                (range.EndIsInsideWord && result.Start + length >= textLength))
            {
                continue;
            }

            normalized.Add(result with { Start = range.Start + result.Start, Length = length });
        }
    }

    private static IReadOnlyList<string> NormalizeSuggestions(string word, IReadOnlyList<string> suggestions)
    {
        if (suggestions.Count == 0)
        {
            return Array.Empty<string>();
        }

        var normalized = new List<string>(Math.Min(suggestions.Count, MaxSuggestionCount));
        var seen = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase)
        {
            word
        };

        foreach (var suggestion in suggestions)
        {
            var value = suggestion.Trim();

            if (value.Length == 0 || !seen.Add(value))
            {
                continue;
            }

            normalized.Add(value);

            if (normalized.Count == MaxSuggestionCount)
            {
                break;
            }
        }

        return normalized.Count == 0 ? Array.Empty<string>() : normalized;
    }

    private readonly struct TextRange
    {
        public TextRange(
            int start,
            int end,
            bool startIsInsideWord = false,
            bool endIsInsideWord = false)
        {
            Start = start;
            End = end;
            StartIsInsideWord = startIsInsideWord;
            EndIsInsideWord = endIsInsideWord;
        }

        public int Start { get; }

        public int End { get; }

        public bool StartIsInsideWord { get; }

        public bool EndIsInsideWord { get; }
    }
}
