using System;
using System.Collections.Generic;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Utils;
using Avalonia.Utilities;

#nullable enable

namespace Avalonia.Automation.Peers
{
    /// <summary>
    /// An implementation of <see cref="ITextRangeProvider"/> that works by character index.
    /// </summary>
    internal class AutomationTextRange : ITextRangeProvider
    {
        private readonly ITextPeer _peer;
        private int _start;
        private int _end;

        public AutomationTextRange(ITextPeer peer, int start, int end)
        {
            if (start < 0 || end < start)
                throw new InvalidOperationException("Invalid text range");

            _peer = peer;
            _start = start;
            _end = end;
        }

        public int Start
        {
            get => _start;
            private set
            {
                if (value < 0)
                    throw new InvalidOperationException();
                if (value > _end)
                    _end = value;
                _start = value;
            }
        }

        public int End
        {
            get => _end;
            private set
            {
                if (value < 0)
                    throw new InvalidOperationException();
                if (value < _start)
                    _start = value;
                _end = value;
            }
        }

        public int Length => _end - _start;

        public override string ToString() => $"[{Start}..{End}]";

        public ITextRangeProvider Clone() => new AutomationTextRange(_peer, Start, End);

        public bool Compare(ITextRangeProvider range)
        {
            var other = range as AutomationTextRange ?? throw new InvalidOperationException("Invalid text range");
            var result = other.Start == Start && other.End == End;
            return result;
        }

        public int CompareEndpoints(
            TextPatternRangeEndpoint endpoint,
            ITextRangeProvider targetRange,
            TextPatternRangeEndpoint targetEndpoint)
        {
            var other = targetRange as AutomationTextRange ?? throw new InvalidOperationException("Invalid text range");
            var e1 = (endpoint == TextPatternRangeEndpoint.Start) ? Start : End;
            var e2 = (targetEndpoint == TextPatternRangeEndpoint.Start) ? other.Start : other.End;
            return e1 - e2;
        }

        public void ExpandToEnclosingUnit(TextUnit unit)
        {
            var text = _peer.Text;

            switch (unit)
            {
                case TextUnit.Character:
                    if (Start == End)
                        End = MoveEndpointForward(text, End, TextUnit.Character, 1, out _);
                    break;

                case TextUnit.Word:
                    // Move start left until we reach a word boundary.
                    for (; !AtWordBoundary(text, Start); Start--);

                    // Move end right until we reach word boundary (different from Start).
                    End = Math.Min(Math.Max(End, Start + 1), text.Length);
                    for (; !AtWordBoundary(text, End); End++);
                    break;

                case TextUnit.Line:
                    if (_peer.LineCount != 1)
                    {
                        int startLine = _peer.LineFromChar(Start);
                        int endLine = _peer.LineFromChar(End);

                        MoveTo(_peer.LineIndex(startLine), _peer.LineIndex(endLine + 1));
                    }
                    else
                    {
                        MoveTo(0, text.Length);
                    }
                    break;

                case TextUnit.Paragraph:
                    // Move start left until we reach a paragraph boundary.
                    for (; !AtParagraphBoundary(text, Start); Start--);

                    // Move end right until we reach a paragraph boundary (different from Start).
                    End = Math.Min(Math.Max(End, Start + 1), text.Length);
                    for (; !AtParagraphBoundary(text, End); End++);
                    break;

                case TextUnit.Format:
                case TextUnit.Page:
                case TextUnit.Document:
                    MoveTo(0, text.Length);
                    break;

                default:
                    throw new ArgumentException("Invalid TextUnit.", nameof(unit));
            }
        }

        public ITextRangeProvider? FindAttribute(
            AvaloniaProperty attribute,
            object value,
            bool backwards) => null;

        public ITextRangeProvider? FindText(string text, bool backwards, bool ignoreCase)
        {
            if (text == null)
                throw new ArgumentNullException("text");
            if (text.Length == 0)
                throw new ArgumentException("Invalid text", nameof(text));

            var rangeText = _peer.Text.Substring(Start, Length);

            if (ignoreCase)
            {
                rangeText = rangeText.ToLower(System.Globalization.CultureInfo.InvariantCulture);
                text = text.ToLower(System.Globalization.CultureInfo.InvariantCulture);
            }

            var i = backwards ?
                rangeText.LastIndexOf(text, StringComparison.Ordinal) :
                rangeText.IndexOf(text, StringComparison.Ordinal);

            return i >= 0 ? new AutomationTextRange(_peer, Start + i, Start + i + text.Length) : null;
        }

        public object? GetAttributeValue(AvaloniaProperty attribute)
        {
            if (attribute == TextBox.IsReadOnlyProperty)
                return _peer.IsReadOnly;
            return null;
        }

        public IReadOnlyList<Rect> GetBoundingRectangles() => _peer.GetBounds(_start, _end);
        public AutomationPeer GetEnclosingElement() => (AutomationPeer)_peer;

        public string GetText(int maxLength)
        {
            return _peer.Text.Substring(Start, maxLength >= 0 ? Math.Min(Length, maxLength) : Length);
        }

        public int Move(TextUnit unit, int count)
        {
            if (count == 0)
                return 0;

            var text = _peer.Text;

            // Save the start and end in case the move fails.
            var oldStart = Start;
            var oldEnd = End;
            var wasDegenerate = Start == End;

            // Move the start of the text range forward or backward in the document by the
            // requested number of text unit boundaries.
            var moved = MoveEndpointByUnit(TextPatternRangeEndpoint.Start, unit, count);
            var succeeded = moved != 0;

            if (succeeded)
            {
                // Make the range degenerate at the new start point.
                End = Start;

                // If we previously had a non-degenerate range then expand the range.
                if (!wasDegenerate)
                {
                    var forwards = count > 0;

                    if (forwards && Start == text.Length - 1)
                    {
                        // The start is at the end of the document, so move the start backward by
                        // one text unit to expand the text range from the degenerate range state.
                        Start = MoveEndpointBackward(text, Start, unit, -1, out var expandMoved);
                        --moved;
                        succeeded = expandMoved == -1 && moved > 0;
                    }
                    else
                    {
                        // The start is not at the end of the document, so move the endpoint
                        // forward by one text unit to expand the text range from the degenerate
                        // state.
                        End = MoveEndpointForward(text, End, unit, 1, out var expandMoved);
                        succeeded = expandMoved > 0;
                    }
                }
            }

            if (!succeeded)
            {
                Start = oldStart;
                End = oldEnd;
                moved = 0;
            }

            return moved;
        }

        public int MoveEndpointByUnit(TextPatternRangeEndpoint endpoint, TextUnit unit, int count)
        {
            if (count == 0)
                return 0;

            var text = _peer.Text;
            var moved = 0;
            var moveStart = endpoint == TextPatternRangeEndpoint.Start;

            if (count > 0)
            {
                if (moveStart)
                    Start = MoveEndpointForward(text, Start, unit, count, out moved);
                else
                    End = MoveEndpointForward(text, End, unit, count, out moved);
            }
            else if (count < 0)
            {
                if (moveStart)
                    Start = MoveEndpointBackward(text, Start, unit, count, out moved);
                else
                    End = MoveEndpointBackward(text, End, unit, count, out moved);
            }

            return moved;
        }

        public void MoveEndpointByRange(
            TextPatternRangeEndpoint endpoint,
            ITextRangeProvider targetRange,
            TextPatternRangeEndpoint targetEndpoint)
        {
            var editRange = targetRange as AutomationTextRange ?? throw new InvalidOperationException("Invalid text range");
            var e = (targetEndpoint == TextPatternRangeEndpoint.Start) ? editRange.Start : editRange.End;

            if (endpoint == TextPatternRangeEndpoint.Start)
                Start = e;
            else
                End = e;
        }

        public void Select() => _peer.Select(Start, End);
        public void AddToSelection() => throw new NotSupportedException();
        public void RemoveFromSelection() => throw new InvalidOperationException();

        public void ScrollIntoView(bool alignToTop) => _peer.ScrollIntoView(Start, End);
        public IReadOnlyList<AutomationPeer> GetChildren() => Array.Empty<AutomationPeer>();

        private static bool AtParagraphBoundary(string text, int index)
        {
            return index <= 0 || index >= text.Length || (text[index] == '\r') && (text[index] != '\n');
        }

        // returns true iff index identifies a word boundary within text.
        // following richedit & word precedent the boundaries are at the leading edge of the word
        // so the span of a word includes trailing whitespace.
        private static bool AtWordBoundary(string text, int index)
        {
            // we are at a word boundary if we are at the beginning or end of the text
            if (index <= 0 || index >= text.Length)
            {
                return true;
            }

            if (AtParagraphBoundary(text, index))
            {
                return true;
            }

            var ch1 = text[index - 1];
            var ch2 = text[index];

            // an apostrophe does *not* break a word if it follows or precedes characters
            if ((char.IsLetterOrDigit(ch1) && IsApostrophe(ch2))
                || (IsApostrophe(ch1) && char.IsLetterOrDigit(ch2) && index >= 2 && char.IsLetterOrDigit(text[index - 2])))
            {
                return false;
            }

            // the following transitions mark boundaries.
            // note: these are constructed to include trailing whitespace.
            return (char.IsWhiteSpace(ch1) && !char.IsWhiteSpace(ch2))
                || (char.IsLetterOrDigit(ch1) && !char.IsLetterOrDigit(ch2))
                || (!char.IsLetterOrDigit(ch1) && char.IsLetterOrDigit(ch2))
                || (char.IsPunctuation(ch1) && char.IsWhiteSpace(ch2));
        }

        private static bool IsApostrophe(char ch)
        {
            return ch == '\'' ||
                   ch == (char)0x2019; // Unicode Right Single Quote Mark
        }

        private int MoveEndpointForward(string text, int index, TextUnit unit, int count, out int moved)
        {
            var limit = text.Length;

            switch (unit)
            {
                case TextUnit.Character:
                    moved = Math.Min(count, limit - index);
                    index = index + moved;
                    index = index > limit ? limit : index;
                    break;

                case TextUnit.Word:
                    for (moved = 0; moved < count && index < text.Length; moved++)
                    {
                        for (index++; !AtWordBoundary(text, index); index++);
                    }
                    break;

                case TextUnit.Line:
                    {
                        var line = _peer.LineFromChar(index);
                        var newLine = MathUtilities.Clamp(line + count, 0, _peer.LineCount - 1);
                        index = _peer.LineIndex(newLine);
                        moved = newLine - line;
                    }
                    break;

                case TextUnit.Paragraph:
                    for (moved = 0; moved < count && index < text.Length; moved++)
                    {
                        for (index++; !AtParagraphBoundary(text, index); index++);
                    }
                    break;

                case TextUnit.Format:
                case TextUnit.Page:
                case TextUnit.Document:
                    moved = index < limit ? 1 : 0;
                    index = limit;
                    break;

                default:
                    throw new ArgumentException("Invalid TextUnit.", nameof(unit));
            }

            return index;
        }

        // moves an endpoint backward a certain number of units.
        // the endpoint is just an index into the text so it could represent either
        // the endpoint.
        private int MoveEndpointBackward(string text, int index, TextUnit unit, int count, out int moved)
        {
            if (index == 0)
            {
                moved = 0;
                return 0;
            }

            switch (unit)
            {
                case TextUnit.Character:
                    int oneBasedIndex = index + 1;
                    moved = Math.Max(count, -oneBasedIndex);
                    index += moved;
                    index = index < 0 ? 0 : index;
                    break;

                case TextUnit.Word:
                    for (moved = 0; moved > count && index > 0; moved--)
                    {
                        for (index--; !AtWordBoundary(text, index); index--);
                    }
                    break;

                case TextUnit.Line:
                    {
                        var line = _peer.LineFromChar(index);

                        // If a line other than the first consists of only a newline, then you can
                        // move backwards past this line and the position changes, hence this is
                        // counted. The first line is special, though: if it is empty, and you move
                        // say from the second line back up to the first, you cannot move further.
                        // However if the first line is nonempty, you can move from the end of the
                        // first line to its beginning! This latter move is counted, but if the
                        // first line is empty, it is not counted.
                        if (line == 0)
                        {
                            index = 0;
                            moved = !StringUtils.IsEol(text[0]) ? -1 : 0;
                        }
                        else
                        {
                            var newLine = MathUtilities.Clamp(line + count, 0, _peer.LineCount - 1);
                            index = _peer.LineIndex(newLine);
                            moved = newLine - line;
                        }
                    }
                    break;

                case TextUnit.Paragraph:
                    for (moved = 0; moved > count && index > 0; moved--)
                    {
                        for (index--; !AtParagraphBoundary(text, index); index--);
                    }
                    break;

                case TextUnit.Format:
                case TextUnit.Page:
                case TextUnit.Document:
                    moved = index > 0 ? -1 : 0;
                    index = 0;
                    break;

                default:
                    throw new ArgumentException("Invalid TextUnit.", nameof(unit));
            }

            return index;
        }

        private void MoveTo(int start, int end)
        {
            if (start < 0 || end < start)
                throw new InvalidOperationException();
            _start = start;
            _end = end;
        }
    }
}

