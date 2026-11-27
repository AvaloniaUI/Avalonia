using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Win32.Automation.Interop;

namespace Avalonia.Win32.Automation;

// WPF's TextBox peer returns a dedicated TextAdaptor for TextPattern. Keep this provider
// separate from AutomationNode so ValuePattern and the raw element work independently.
[GeneratedComClass]
internal partial class TextBoxTextProvider : ITextProvider
{
    private readonly AutomationNode _element;
    private readonly TextBox _owner;

    public TextBoxTextProvider(AutomationNode element, TextBox owner)
    {
        _element = element;
        _owner = owner;
    }

    public ITextRangeProvider[] GetSelection() => OnUi(() =>
    {
        var start = Math.Min(_owner.SelectionStart, _owner.SelectionEnd);
        var end = Math.Max(_owner.SelectionStart, _owner.SelectionEnd);
        return new ITextRangeProvider[] { Range(start, end) };
    });

    public ITextRangeProvider[] GetVisibleRanges() => OnUi(() =>
        new ITextRangeProvider[] { Range(0, DisplayedText.Length) });

    public ITextRangeProvider RangeFromChild(IRawElementProviderSimple childElement) =>
        throw new COMException("The TextBox has no text child elements.", UiaCoreProviderApi.UIA_E_ELEMENTNOTAVAILABLE);

    public ITextRangeProvider RangeFromPoint(double X, double Y) => OnUi(() =>
    {
        var presenter = _owner.FindDescendantOfType<TextPresenter>();
        if (presenter is null)
            return Range(_owner.CaretIndex, _owner.CaretIndex);

        var clientPoint = _owner.PointToClient(new PixelPoint((int)Math.Round(X), (int)Math.Round(Y)));
        var presenterPoint = _owner.TranslatePoint(clientPoint, presenter) ?? default;
        var index = presenter.TextLayout.HitTestPoint(presenterPoint).TextPosition;
        return Range(index, index);
    });

    public ITextRangeProvider GetDocumentRange() => OnUi(() => Range(0, DisplayedText.Length));
    public SupportedTextSelection GetSupportedTextSelection() => SupportedTextSelection.Single;

    private string DisplayedText => _owner.PasswordChar == default || _owner.RevealPassword
        ? _owner.Text ?? string.Empty
        : new string(_owner.PasswordChar, _owner.Text?.Length ?? 0);

    private TextBoxTextRangeProvider Range(int start, int end) => new(_element, _owner, start, end);

    private static T OnUi<T>(Func<T> action) => Dispatcher.UIThread.CheckAccess()
        ? action()
        : Dispatcher.UIThread.InvokeAsync(action).GetAwaiter().GetResult();
}

[GeneratedComClass]
internal partial class TextBoxTextRangeProvider : ITextRangeProvider
{
    private readonly AutomationNode _element;
    private readonly TextBox _owner;
    private int _start;
    private int _end;

    public TextBoxTextRangeProvider(AutomationNode element, TextBox owner, int start, int end)
    {
        _element = element;
        _owner = owner;
        _start = Clamp(Math.Min(start, end));
        _end = Clamp(Math.Max(start, end));
    }

    public ITextRangeProvider Clone() => OnUi(() => new TextBoxTextRangeProvider(_element, _owner, _start, _end));

    public bool Compare(ITextRangeProvider range) => OnUi(() =>
        range is TextBoxTextRangeProvider other && ReferenceEquals(_owner, other._owner) &&
        _start == other._start && _end == other._end);

    public int CompareEndpoints(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint) => OnUi(() =>
    {
        if (targetRange is not TextBoxTextRangeProvider other || !ReferenceEquals(_owner, other._owner))
            throw new COMException("The ranges belong to different text providers.", UiaCoreProviderApi.UIA_E_ELEMENTNOTAVAILABLE);
        return Endpoint(endpoint).CompareTo(other.Endpoint(targetEndpoint));
    });

    public void ExpandToEnclosingUnit(TextUnit unit) => OnUi(() =>
    {
        var text = Text;
        if (unit is TextUnit.Document or TextUnit.Page or TextUnit.Paragraph)
        {
            _start = 0;
            _end = text.Length;
        }
        else if (unit == TextUnit.Character)
        {
            _start = Clamp(_start);
            _end = NextCharacter(text, _start);
        }
        else if (unit == TextUnit.Word)
        {
            _start = Clamp(_start);
            _end = Clamp(_end);
            while (_start > 0 && !char.IsWhiteSpace(text[_start - 1])) _start = PreviousCharacter(text, _start);
            while (_end < text.Length && !char.IsWhiteSpace(text[_end])) _end = NextCharacter(text, _end);
        }
        else
        {
            var caret = Clamp(_start);
            var startBreak = caret == 0 ? -1 : text.LastIndexOf('\n', caret - 1);
            var endBreak = text.IndexOf('\n', caret);
            _start = startBreak + 1;
            _end = endBreak < 0 ? text.Length : endBreak;
        }
    });

    public ITextRangeProvider FindAttribute(int attribute, object value, bool backward) => null!;

    public ITextRangeProvider FindText(string text, bool backward, bool ignoreCase) => OnUi<ITextRangeProvider>(() =>
    {
        if (string.IsNullOrEmpty(text)) return null!;
        var comparison = ignoreCase ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture;
        var index = backward
            ? Text.LastIndexOf(text, Math.Max(0, _start - 1), _start, comparison)
            : Text.IndexOf(text, _end, comparison);
        return index < 0 ? null! : new TextBoxTextRangeProvider(_element, _owner, index, index + text.Length);
    });

    [SupportedOSPlatform("windows")]
    public object GetAttributeValue(int attribute) => UiaCoreProviderApi.GetReservedNotSupportedValue();

    public double[] GetBoundingRectangles() => OnUi(() =>
    {
        var presenter = _owner.FindDescendantOfType<TextPresenter>();
        if (presenter is null) return Array.Empty<double>();

        var rects = _start == _end
            ? new[] { presenter.TextLayout.HitTestTextPosition(_start) }
            : presenter.TextLayout.HitTestTextRange(_start, _end - _start).ToArray();
        var bounds = new double[rects.Length * 4];
        for (var i = 0; i < rects.Length; i++)
        {
            var topLeft = presenter.PointToScreen(rects[i].TopLeft);
            var bottomRight = presenter.PointToScreen(rects[i].BottomRight);
            bounds[i * 4] = topLeft.X;
            bounds[i * 4 + 1] = topLeft.Y;
            bounds[i * 4 + 2] = bottomRight.X - topLeft.X;
            bounds[i * 4 + 3] = bottomRight.Y - topLeft.Y;
        }
        return bounds;
    });

    public IRawElementProviderSimple GetEnclosingElement() => _element;

    public string GetText(int maxLength) => OnUi(() =>
    {
        var text = DisplayedText;
        var start = Math.Min(_start, text.Length);
        var length = Math.Max(0, Math.Min(_end, text.Length) - start);
        var result = text.Substring(start, length);
        return maxLength >= 0 && result.Length > maxLength ? result.Substring(0, maxLength) : result;
    });

    public int Move(TextUnit unit, int count) => OnUi(() =>
    {
        var originalStart = _start;
        var originalEnd = _end;
        var text = Text;
        var remaining = count;
        if (unit is TextUnit.Document or TextUnit.Page or TextUnit.Paragraph)
            _start = count < 0 ? 0 : text.Length;
        else
        {
            while (remaining < 0 && _start > 0)
            {
                var next = MoveOneUnit(text, _start, unit, -1);
                if (next == _start) break;
                _start = next;
                remaining++;
            }
            while (remaining > 0 && _start < text.Length)
            {
                var next = MoveOneUnit(text, _start, unit, 1);
                if (next == _start) break;
                _start = next;
                remaining--;
            }
        }
        _end = Clamp(_start + originalEnd - originalStart);
        return count - remaining;
    });

    public int MoveEndpointByUnit(TextPatternRangeEndpoint endpoint, TextUnit unit, int count) => OnUi(() =>
    {
        var position = Endpoint(endpoint);
        var remaining = count;
        if (unit is TextUnit.Document or TextUnit.Page or TextUnit.Paragraph)
            position = count < 0 ? 0 : Text.Length;
        else
        {
            while (remaining < 0 && position > 0)
            {
                var next = MoveOneUnit(Text, position, unit, -1);
                if (next == position) break;
                position = next;
                remaining++;
            }
            while (remaining > 0 && position < Text.Length)
            {
                var next = MoveOneUnit(Text, position, unit, 1);
                if (next == position) break;
                position = next;
                remaining--;
            }
        }
        SetEndpoint(endpoint, position);
        return count - remaining;
    });

    public void MoveEndpointByRange(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint) => OnUi(() =>
    {
        if (targetRange is not TextBoxTextRangeProvider other || !ReferenceEquals(_owner, other._owner))
            throw new COMException("The ranges belong to different text providers.", UiaCoreProviderApi.UIA_E_ELEMENTNOTAVAILABLE);
        SetEndpoint(endpoint, other.Endpoint(targetEndpoint));
    });

    public void Select() => OnUi(() =>
    {
        _owner.SelectionStart = _start;
        _owner.SelectionEnd = _end;
        _owner.CaretIndex = _end;
    });

    // TextBox has one text selection. UIA clients replace it with Select(); the
    // additive/removal operations are undefined for a single selection, as in WPF.
    public void AddToSelection() => throw new InvalidOperationException();
    public void RemoveFromSelection() => throw new InvalidOperationException();
    public void ScrollIntoView(bool alignToTop) => OnUi(() => _owner.BringIntoView());
    public IRawElementProviderSimple[] GetChildren() => Array.Empty<IRawElementProviderSimple>();

    private string Text => _owner.Text ?? string.Empty;
    private string DisplayedText => _owner.PasswordChar == default || _owner.RevealPassword
        ? Text
        : new string(_owner.PasswordChar, Text.Length);
    private int Clamp(int index) => Math.Clamp(index, 0, Text.Length);
    private int Endpoint(TextPatternRangeEndpoint endpoint) => endpoint == TextPatternRangeEndpoint.Start ? _start : _end;

    private void SetEndpoint(TextPatternRangeEndpoint endpoint, int value)
    {
        value = Clamp(value);
        if (endpoint == TextPatternRangeEndpoint.Start) _start = Math.Min(value, _end);
        else _end = Math.Max(value, _start);
    }

    private static int MoveOneUnit(string text, int position, TextUnit unit, int direction)
    {
        if (unit == TextUnit.Character)
            return direction < 0 ? PreviousCharacter(text, position) : NextCharacter(text, position);
        if (unit == TextUnit.Word)
        {
            if (direction < 0)
            {
                var wordIndex = PreviousCharacter(text, position);
                while (wordIndex > 0 && char.IsWhiteSpace(text[wordIndex])) wordIndex = PreviousCharacter(text, wordIndex);
                while (wordIndex > 0 && !char.IsWhiteSpace(text[PreviousCharacter(text, wordIndex)])) wordIndex = PreviousCharacter(text, wordIndex);
                return wordIndex;
            }
            var i = position;
            while (i < text.Length && !char.IsWhiteSpace(text[i])) i = NextCharacter(text, i);
            while (i < text.Length && char.IsWhiteSpace(text[i])) i = NextCharacter(text, i);
            return i;
        }
        if (unit is TextUnit.Line or TextUnit.Paragraph)
        {
            if (direction < 0)
            {
                var currentBreak = position == 0 ? -1 : text.LastIndexOf('\n', position - 1);
                if (currentBreak < 0) return 0;
                var previousBreak = currentBreak == 0 ? -1 : text.LastIndexOf('\n', currentBreak - 1);
                return previousBreak + 1;
            }
            var nextBreak = text.IndexOf('\n', position);
            return nextBreak < 0 ? text.Length : nextBreak + 1;
        }
        // A plain TextBox has no formatting runs, so Format spans the document.
        return direction < 0 ? 0 : text.Length;
    }

    private static int PreviousCharacter(string text, int position)
    {
        position = Math.Clamp(position, 0, text.Length);
        return position >= 2 && char.IsLowSurrogate(text[position - 1]) && char.IsHighSurrogate(text[position - 2])
            ? position - 2
            : Math.Max(0, position - 1);
    }

    private static int NextCharacter(string text, int position)
    {
        position = Math.Clamp(position, 0, text.Length);
        return position + 1 < text.Length && char.IsHighSurrogate(text[position]) && char.IsLowSurrogate(text[position + 1])
            ? position + 2
            : Math.Min(text.Length, position + 1);
    }

    private static void OnUi(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess()) action();
        else Dispatcher.UIThread.InvokeAsync(action).GetAwaiter().GetResult();
    }

    private static T OnUi<T>(Func<T> action) => Dispatcher.UIThread.CheckAccess()
        ? action()
        : Dispatcher.UIThread.InvokeAsync(action).GetAwaiter().GetResult();
}
