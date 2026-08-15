using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation.Provider;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Utils;
using Avalonia.VisualTree;

namespace Avalonia.Automation.Peers
{
    public partial class TextBoxAutomationPeer
    {
        /// <summary>
        /// A <c>[start,end)</c> character-index range into a <see cref="Controls.TextBox"/>'s text,
        /// implementing <see cref="ITextRangeProvider"/> against <see cref="TextPresenter"/>'s
        /// text layout. <see cref="_start"/> and <see cref="_end"/> are always kept normalized
        /// (<c>_start &lt;= _end</c>), clamped to <c>[0, Text.Length]</c>.
        /// </summary>
        private sealed class TextRange : ITextRangeProvider
        {
            private readonly TextBoxAutomationPeer _peer;
            private int _start;
            private int _end;

            public TextRange(TextBoxAutomationPeer peer, int start, int end)
            {
                _peer = peer;
                Normalize(start, end);
            }

            private string Text => _peer.Owner.Text ?? string.Empty;

            private void Normalize(int start, int end)
            {
                if (start > end)
                    (start, end) = (end, start);

                var length = Text.Length;
                _start = Math.Clamp(start, 0, length);
                _end = Math.Clamp(end, 0, length);
            }

            private int GetEndpoint(TextPatternRangeEndpoint endpoint) => endpoint == TextPatternRangeEndpoint.Start ? _start : _end;

            public ITextRangeProvider Clone() => new TextRange(_peer, _start, _end);

            public bool Compare(ITextRangeProvider range) =>
                range is TextRange other && other._peer == _peer && other._start == _start && other._end == _end;

            public int CompareEndpoints(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint)
            {
                var target = (TextRange)targetRange;
                return GetEndpoint(endpoint).CompareTo(target.GetEndpoint(targetEndpoint));
            }

            public void ExpandToEnclosingUnit(TextUnit unit)
            {
                var text = Text;
                var caretIndex = Math.Min(_start, text.Length);

                switch (unit)
                {
                    case TextUnit.Character:
                        _start = caretIndex;
                        _end = Math.Min(caretIndex + 1, text.Length);
                        break;

                    case TextUnit.Word:
                    {
                        var start = caretIndex;
                        var end = caretIndex;

                        if (!StringUtils.IsStartOfWord(text, caretIndex))
                            start = StringUtils.PreviousWord(text, caretIndex);

                        if (!StringUtils.IsEndOfWord(text, caretIndex))
                            end = StringUtils.NextWord(text, caretIndex);

                        Normalize(start, Math.Max(start, end));
                        break;
                    }

                    case TextUnit.Line:
                    {
                        var (lineStart, lineEnd) = GetLineBounds(caretIndex);
                        Normalize(lineStart, lineEnd);
                        break;
                    }

                    case TextUnit.Paragraph:
                    {
                        var (paraStart, paraEnd) = GetParagraphBounds(text, caretIndex);
                        Normalize(paraStart, paraEnd);
                        break;
                    }

                    // Format/Page/Document degenerate to the whole document: TextBox has no
                    // formatting-attribute runs or pagination.
                    case TextUnit.Format:
                    case TextUnit.Page:
                    case TextUnit.Document:
                    default:
                        Normalize(0, text.Length);
                        break;
                }
            }

            public ITextRangeProvider? FindAttribute(int attribute, object? value, bool backward) => null;

            public ITextRangeProvider? FindText(string text, bool backward, bool ignoreCase)
            {
                var haystack = Text.Substring(_start, _end - _start);
                var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                var index = backward
                    ? haystack.LastIndexOf(text, comparison)
                    : haystack.IndexOf(text, comparison);

                if (index < 0)
                    return null;

                var start = _start + index;
                return new TextRange(_peer, start, start + text.Length);
            }

            public object GetAttributeValue(int attribute) => AutomationTextAttributeNotSupported.Instance;

            public IReadOnlyList<Rect> GetBoundingRectangles()
            {
                var presenter = _peer.Owner.Presenter;
                if (presenter is null || _start == _end)
                    return Array.Empty<Rect>();

                if (presenter.GetVisualRoot() is not Visual root)
                    return Array.Empty<Rect>();

                var transform = presenter.TransformToVisual(root);
                if (transform is null)
                    return Array.Empty<Rect>();

                var rects = presenter.TextLayout.HitTestTextRange(_start, _end - _start);
                var result = new List<Rect>();

                foreach (var rect in rects)
                {
                    var snapped = PixelRect.FromRect(rect, 1).ToRect(1);
                    result.Add(snapped.TransformToAABB(transform.Value));
                }

                return result;
            }

            public AutomationPeer GetEnclosingElement() => _peer;

            public string GetText(int maxLength)
            {
                var text = Text.Substring(_start, _end - _start);
                return maxLength >= 0 && text.Length > maxLength ? text.Substring(0, maxLength) : text;
            }

            public int Move(TextUnit unit, int count)
            {
                if (count == 0)
                    return 0;

                var length = _end - _start;
                var (newStart, moved) = MoveIndex(_start, unit, count);

                Normalize(newStart, newStart + length);
                return moved;
            }

            public int MoveEndpointByUnit(TextPatternRangeEndpoint endpoint, TextUnit unit, int count)
            {
                if (count == 0)
                    return 0;

                var (newValue, moved) = MoveIndex(GetEndpoint(endpoint), unit, count);

                if (endpoint == TextPatternRangeEndpoint.Start)
                    Normalize(newValue, Math.Max(newValue, _end));
                else
                    Normalize(Math.Min(_start, newValue), newValue);

                return moved;
            }

            public void MoveEndpointByRange(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint)
            {
                var target = (TextRange)targetRange;
                var value = target.GetEndpoint(targetEndpoint);

                if (endpoint == TextPatternRangeEndpoint.Start)
                    Normalize(value, Math.Max(value, _end));
                else
                    Normalize(Math.Min(_start, value), value);
            }

            public void Select()
            {
                var owner = _peer.Owner;
                // CaretIndex's setter collapses SelectionStart/SelectionEnd to itself, so it must
                // be set first; setting SelectionStart/SelectionEnd afterwards only moves
                // CaretIndex back when the two endpoints end up equal (see TextBox's
                // OnSelectionStartChanged/OnSelectionEndChanged), so this order preserves the
                // intended [_start, _end) selection with the caret left at _end.
                owner.CaretIndex = _end;
                owner.SelectionStart = _start;
                owner.SelectionEnd = _end;
            }

            public void AddToSelection() => Select();

            public void RemoveFromSelection() => throw new NotSupportedException();

            public void ScrollIntoView(bool alignToTop) => _peer.BringIntoView();

            public IReadOnlyList<AutomationPeer> GetChildren() => Array.Empty<AutomationPeer>();

            private (int NewIndex, int Moved) MoveIndex(int index, TextUnit unit, int count)
            {
                var text = Text;

                switch (unit)
                {
                    case TextUnit.Line:
                        return MoveByLine(index, count);

                    case TextUnit.Word:
                        return MoveByWord(text, index, count);

                    case TextUnit.Format:
                    case TextUnit.Paragraph:
                    case TextUnit.Page:
                    case TextUnit.Document:
                    {
                        // Degenerates to Document: a single move jumps to the corresponding end.
                        var target = count > 0 ? text.Length : 0;
                        var moved = target == index ? 0 : (count > 0 ? 1 : -1);
                        return (target, moved);
                    }

                    case TextUnit.Character:
                    default:
                    {
                        var target = Math.Clamp(index + count, 0, text.Length);
                        return (target, target - index);
                    }
                }
            }

            private (int NewIndex, int Moved) MoveByWord(string text, int index, int count)
            {
                var pos = index;
                var moved = 0;

                if (count > 0)
                {
                    for (var i = 0; i < count; i++)
                    {
                        var next = StringUtils.NextWord(text, pos);
                        if (next == pos)
                            break;
                        pos = next;
                        moved++;
                    }
                }
                else
                {
                    for (var i = 0; i < -count; i++)
                    {
                        var prev = StringUtils.PreviousWord(text, pos);
                        if (prev == pos)
                            break;
                        pos = prev;
                        moved--;
                    }
                }

                return (pos, moved);
            }

            private (int NewIndex, int Moved) MoveByLine(int index, int count)
            {
                var presenter = _peer.Owner.Presenter;
                if (presenter is null)
                    return (index, 0);

                var layout = presenter.TextLayout;
                var lineIndex = layout.GetLineIndexFromCharacterIndex(index, false);
                var targetLine = Math.Clamp(lineIndex + count, 0, layout.TextLines.Count - 1);
                var moved = targetLine - lineIndex;

                return (layout.TextLines[targetLine].FirstTextSourceIndex, moved);
            }

            private (int Start, int End) GetLineBounds(int index)
            {
                var presenter = _peer.Owner.Presenter;
                if (presenter is null)
                    return (index, index);

                var layout = presenter.TextLayout;
                var lineIndex = layout.GetLineIndexFromCharacterIndex(index, false);
                var line = layout.TextLines[lineIndex];

                return (line.FirstTextSourceIndex, line.FirstTextSourceIndex + line.Length);
            }

            private static (int Start, int End) GetParagraphBounds(string text, int index)
            {
                if (text.Length == 0)
                    return (0, 0);

                var start = text.LastIndexOf('\n', Math.Max(0, Math.Min(index, text.Length) - 1)) + 1;
                var end = text.IndexOf('\n', Math.Min(index, text.Length));
                if (end < 0)
                    end = text.Length;
                else
                    end += 1;

                return (start, end);
            }
        }
    }
}
