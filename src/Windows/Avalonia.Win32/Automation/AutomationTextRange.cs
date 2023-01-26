using System;
using System.Runtime.InteropServices;
using Avalonia.Automation.Provider;
using Avalonia.Utilities;
using Avalonia.Win32.Interop.Automation;
using AAP = Avalonia.Automation.Provider;

#nullable enable

namespace Avalonia.Win32.Automation
{
    internal class AutomationTextRange : ITextRangeProvider
    {
        private readonly AutomationNode _owner;

        public AutomationTextRange(AutomationNode owner, TextRange range)
            : this(owner, range.Start, range.End)
        {
        }

        public AutomationTextRange(AutomationNode owner, int start, int end)
        {
            _owner = owner;
            Start = start;
            End = end;
        }

        public int Start { get; private set; }
        public int End { get; private set; }
        public TextRange Range => TextRange.FromInclusiveStartEnd(Start, End);

        private AAP.ITextProvider InnerProvider => (AAP.ITextProvider)_owner.Peer;

        public ITextRangeProvider Clone() => new AutomationTextRange(_owner, Range);

        [return: MarshalAs(UnmanagedType.Bool)]
        public bool Compare(ITextRangeProvider range)
        {
            return range is AutomationTextRange other && other.Start == Start && other.End == End;
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
            _owner.InvokeSync(() =>
            {
                var text = InnerProvider.GetText(InnerProvider.DocumentRange);

                switch (unit)
                {
                    case TextUnit.Character:
                        if (Start == End)
                            End = MoveEndpointForward(text, End, TextUnit.Character, 1, out _);
                        break;
                    case TextUnit.Word:
                        // Move start left until we reach a word boundary.
                        for (; !AtWordBoundary(text, Start); Start--)
                            ;
                        // Move end right until we reach word boundary (different from Start).
                        End = Math.Min(Math.Max(End, Start + 1), text.Length);
                        for (; !AtWordBoundary(text, End); End++)
                            ;
                        break;
                    case TextUnit.Line:
                        if (InnerProvider.LineCount != 1)
                        {
                            int startLine = InnerProvider.GetLineForIndex(Start);
                            int endLine = InnerProvider.GetLineForIndex(End);
                            var start = InnerProvider.GetLineRange(startLine).Start;
                            var end = InnerProvider.GetLineRange(endLine).End;
                            MoveTo(start, end);
                        }
                        else
                        {
                            MoveTo(0, text.Length);
                        }
                        break;
                    case TextUnit.Paragraph:
                        // Move start left until we reach a paragraph boundary.
                        for (; !AtParagraphBoundary(text, Start); Start--)
                            ;
                        // Move end right until we reach a paragraph boundary (different from Start).
                        End = Math.Min(Math.Max(End, Start + 1), text.Length);
                        for (; !AtParagraphBoundary(text, End); End++)
                            ;
                        break;
                    case TextUnit.Format:
                    case TextUnit.Page:
                    case TextUnit.Document:
                        MoveTo(0, text.Length);
                        break;
                    default:
                        throw new ArgumentException("Invalid TextUnit.", nameof(unit));
                }
            });
        }

        public ITextRangeProvider? FindAttribute(
            TextPatternAttribute attribute,
            object? value,
            [MarshalAs(UnmanagedType.Bool)] bool backward)
        {
            // TODO: Implement.
            return null;
        }

        public ITextRangeProvider? FindText(
            string text,
            [MarshalAs(UnmanagedType.Bool)] bool backward,
            [MarshalAs(UnmanagedType.Bool)] bool ignoreCase)
        {
            return _owner.InvokeSync(() =>
            {
                var rangeText = InnerProvider.GetText(Range);

                if (ignoreCase)
                {
                    rangeText = rangeText.ToLowerInvariant();
                    text = text.ToLowerInvariant();
                }

                var i = backward ?
                    rangeText.LastIndexOf(text, StringComparison.Ordinal) :
                    rangeText.IndexOf(text, StringComparison.Ordinal);
                return i >= 0 ? new AutomationTextRange(_owner, Start + i, Start + i + text.Length) : null;
            });
        }

        public object? GetAttributeValue(TextPatternAttribute attribute)
        {
            return attribute switch
            {
                TextPatternAttribute.IsReadOnlyAttributeId => _owner.InvokeSync(() => InnerProvider.IsReadOnly),
                _ => null
            };
        }

        public double[] GetBoundingRectangles()
        {
            return _owner.InvokeSync(() =>
            {
                var rects = InnerProvider.GetBounds(Range);
                var result = new double[rects.Count * 4];
                var root = _owner.GetRoot() as RootAutomationNode;

                if (root is object)
                {
                    for (var i = 0; i < rects.Count; i++)
                    {
                        var screenRect = root.ToScreen(rects[i]);
                        result[4 * i] = screenRect.X;
                        result[4 * i + 1] = screenRect.Y;
                        result[4 * i + 2] = screenRect.Width;
                        result[4 * i + 3] = screenRect.Height;
                    }
                }

                return result;
            });
        }

        public IRawElementProviderSimple[] GetChildren() => Array.Empty<IRawElementProviderSimple>();
        public IRawElementProviderSimple GetEnclosingElement() => _owner;

        public string GetText(int maxLength)
        {
            if (maxLength < 0)
                maxLength = int.MaxValue;
            maxLength = Math.Min(maxLength, End - Start);
            return _owner.InvokeSync(() => InnerProvider.GetText(new TextRange(Start, maxLength)));
        }

        public int Move(TextUnit unit, int count)
        {
            return _owner.InvokeSync(() =>
            {
                if (count == 0)
                    return 0;
                var text = InnerProvider.GetText(new(0, int.MaxValue));
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
            });
        }

        public void MoveEndpointByRange(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint)
        {
            var editRange = targetRange as AutomationTextRange ?? throw new InvalidOperationException("Invalid text range");
            var e = (targetEndpoint == TextPatternRangeEndpoint.Start) ? editRange.Start : editRange.End;

            if (endpoint == TextPatternRangeEndpoint.Start)
                Start = e;
            else
                End = e;
        }

        public int MoveEndpointByUnit(TextPatternRangeEndpoint endpoint, TextUnit unit, int count)
        {
            if (count == 0)
                return 0;

            return _owner.InvokeSync(() =>
            {
                var text = InnerProvider.GetText(InnerProvider.DocumentRange);
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
            });
        }

        void ITextRangeProvider.AddToSelection() => throw new NotSupportedException();
        void ITextRangeProvider.RemoveFromSelection() => throw new NotSupportedException();

        public void ScrollIntoView([MarshalAs(UnmanagedType.Bool)] bool alignToTop)
        {
            _owner.InvokeSync(() => InnerProvider.ScrollIntoView(Range));
        }

        public void Select()
        {
            _owner.InvokeSync(() => InnerProvider.Select(Range));
        }

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

        private static bool IsWordBreak(char ch)
        {
            return char.IsWhiteSpace(ch) || char.IsPunctuation(ch);
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
                    // TODO: This will need to implement the Unicode word boundaries spec.
                    for (moved = 0; moved < count && index < text.Length; moved++)
                    {
                        for (index++; !AtWordBoundary(text, index); index++)
                            ;
                        for (; index < text.Length && IsWordBreak(text[index]); index++)
                            ;
                    }
                    break;

                case TextUnit.Line:
                    {
                        var line = InnerProvider.GetLineForIndex(index);
                        var newLine = MathUtilities.Clamp(line + count, 0, InnerProvider.LineCount - 1);
                        index = InnerProvider.GetLineRange(newLine).Start;
                        moved = newLine - line;
                    }
                    break;

                case TextUnit.Paragraph:
                    for (moved = 0; moved < count && index < text.Length; moved++)
                    {
                        for (index++; !AtParagraphBoundary(text, index); index++)
                            ;
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
                        for (index--; index < text.Length && IsWordBreak(text[index]); index--)
                            ;
                        for (index--; !AtWordBoundary(text, index); index--)
                            ;
                    }
                    break;
                case TextUnit.Line:
                    {
                        var line = InnerProvider.GetLineForIndex(index);
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
                            moved = !IsEol(text[0]) ? -1 : 0;
                        }
                        else
                        {
                            var newLine = MathUtilities.Clamp(line + count, 0, InnerProvider.LineCount - 1);
                            index = InnerProvider.GetLineRange(newLine).Start;
                            moved = newLine - line;
                        }
                    }
                    break;
                case TextUnit.Paragraph:
                    for (moved = 0; moved > count && index > 0; moved--)
                    {
                        for (index--; !AtParagraphBoundary(text, index); index--)
                            ;
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
            Start = start;
            End = end;
        }

        public static bool IsEol(char c)
        {
            return c == '\r' || c == '\n';
        }
    }
}
