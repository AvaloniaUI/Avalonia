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
            GetProvider(owner) is not null;
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

    public void ScheduleCheck()
    {
        var text = _owner.Text;

        if (!CanCheck(_owner) ||
            !CanCheckText(text) ||
            GetProvider(_owner) is null)
        {
            Clear();
            return;
        }

        InvalidateResults();

        var timer = GetCheckTimer();
        timer.Stop();
        timer.Start();
    }

    public void RefreshStyles()
    {
        ApplyResults();
    }

    public void Clear()
    {
        _checkTimer?.Stop();
        InvalidateResults();
    }

    private void InvalidateResults()
    {
        _version++;
        _checkCancellation?.Cancel();
        _checkCancellation = null;
        _results = Array.Empty<SpellCheckResult>();
        _checkedRanges = Array.Empty<TextRange>();
        _checkedText = null;
        _presenter?.SetTextStyleOverrides(null);
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
            !provider.IsLanguageSupported(culture))
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

            SetResults(text, ranges, results);
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

        var culture = CultureInfo.CurrentCulture;

        if (!provider.IsLanguageSupported(culture))
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
            var results = await CheckRangesAsync(text, ranges, provider, culture, cancellation.Token);

            if (cancellation.IsCancellationRequested || version != _version)
            {
                return;
            }

            SetResults(text, ranges, results);
        }
        catch (OperationCanceledException)
        {
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

        if (options.IsSpellCheckEnabled == false ||
            options.IsSensitive ||
            owner.PasswordChar != default ||
            options.ContentType is TextInputContentType.Password or TextInputContentType.Pin)
        {
            return false;
        }

        if (options.IsSpellCheckEnabled == true)
        {
            return true;
        }

        return options.ContentType is TextInputContentType.Normal
            or TextInputContentType.Alpha
            or TextInputContentType.Name
            or TextInputContentType.Search
            or TextInputContentType.Social;
    }

    private static ISpellCheckProvider? GetProvider(TextBox owner)
    {
        return TopLevel.GetTopLevel(owner)?.SpellCheckProvider;
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

    private void SetResults(string text, List<TextRange> ranges, IReadOnlyList<SpellCheckResult> results)
    {
        _results = results;
        _checkedRanges = ranges.Count == 0 ? Array.Empty<TextRange>() : ranges.ToArray();
        _checkedText = text;
        ApplyResults();
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
        TextRunProperties? textRunProperties = null;

        for (var i = 0; i < _results.Count; i++)
        {
            var result = _results[i];

            if (!IntersectsVisibleRange(result, ranges))
            {
                continue;
            }

            textRunProperties ??= CreateMisspellingTextRunProperties(_presenter);
            spans ??= new List<ValueSpan<TextRunProperties>>();
            spans.Add(new ValueSpan<TextRunProperties>(result.Start, result.Length, textRunProperties));
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

        return normalized is null || normalized.Count == 0
            ? Array.Empty<SpellCheckResult>()
            : normalized;
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
