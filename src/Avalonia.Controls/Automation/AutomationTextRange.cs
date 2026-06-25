using System;
using Avalonia.Automation.Provider;
using Avalonia.Input.TextInput;
using Avalonia.Metadata;

namespace Avalonia.Automation
{
    /// <summary>
    /// A mutable text-range cursor over an <see cref="ITextNavigation"/>. Reusable by the platform
    /// accessibility backends (Win32 UIA TextPattern, AT-SPI), which wrap it for their protocol.
    /// </summary>
    [Unstable]
    public sealed class AutomationTextRange : ITextRangeProvider
    {
        private readonly ITextNavigation _navigation;
        private ITextPointer _start;
        private ITextPointer _end;

        public AutomationTextRange(ITextNavigation navigation, ITextPointer start, ITextPointer end)
        {
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));

            if (start is null)
            {
                throw new ArgumentNullException(nameof(start));
            }

            if (end is null)
            {
                throw new ArgumentNullException(nameof(end));
            }

            if (start.CompareTo(end) <= 0)
            {
                _start = start;
                _end = end;
            }
            else
            {
                _start = end;
                _end = start;
            }
        }

        public ITextRangeProvider Clone() => new AutomationTextRange(_navigation, _start, _end);

        public bool Compare(ITextRangeProvider other)
            => other is AutomationTextRange range && _start.Equals(range._start) && _end.Equals(range._end);

        public int CompareEndpoints(TextRangeEndpoint endpoint, ITextRangeProvider other, TextRangeEndpoint otherEndpoint)
        {
            if (other is not AutomationTextRange range)
            {
                throw new ArgumentException("The range was not produced by this provider.", nameof(other));
            }

            var mine = endpoint == TextRangeEndpoint.Start ? _start : _end;
            var theirs = otherEndpoint == TextRangeEndpoint.Start ? range._start : range._end;

            return mine.CompareTo(theirs);
        }

        public void ExpandToEnclosingUnit(TextUnit unit)
        {
            var range = _navigation.GetRangeEnclosing(_start, unit);
            _start = range.Start;
            _end = range.End;
        }

        public string GetText(int maxLength)
        {
            var text = _navigation.GetText(_navigation.GetRange(_start, _end));

            return maxLength >= 0 && text.Length > maxLength ? text.Substring(0, maxLength) : text;
        }

        public int Move(TextUnit unit, int count)
        {
            // Normalize to the enclosing unit, then move that unit by count.
            ExpandToEnclosingUnit(unit);

            var (moved, position) = StepByUnit(_start, unit, count);
            var range = _navigation.GetRangeEnclosing(position, unit);
            _start = range.Start;
            _end = range.End;

            return moved;
        }

        public int MoveEndpointByUnit(TextRangeEndpoint endpoint, TextUnit unit, int count)
        {
            var origin = endpoint == TextRangeEndpoint.Start ? _start : _end;
            var (moved, position) = StepByUnit(origin, unit, count);

            SetEndpoint(endpoint, position);

            return moved;
        }

        public void MoveEndpointByRange(TextRangeEndpoint endpoint, ITextRangeProvider other, TextRangeEndpoint otherEndpoint)
        {
            if (other is not AutomationTextRange range)
            {
                throw new ArgumentException("The range was not produced by this provider.", nameof(other));
            }

            SetEndpoint(endpoint, otherEndpoint == TextRangeEndpoint.Start ? range._start : range._end);
        }

        public void Select()
        {
            if (_navigation is IAccessibleText accessible)
            {
                accessible.SetSelection(_navigation.GetRange(_start, _end));
            }
        }

        // Moves an endpoint; if it passes the other endpoint the range collapses (UIA semantics).
        private void SetEndpoint(TextRangeEndpoint endpoint, ITextPointer position)
        {
            if (endpoint == TextRangeEndpoint.Start)
            {
                _start = position;

                if (_start.CompareTo(_end) > 0)
                {
                    _end = _start;
                }
            }
            else
            {
                _end = position;

                if (_end.CompareTo(_start) < 0)
                {
                    _start = _end;
                }
            }
        }

        private (int Moved, ITextPointer Position) StepByUnit(ITextPointer origin, TextUnit unit, int count)
        {
            var forward = count >= 0;
            var steps = Math.Abs(count);
            var current = origin;
            var moved = 0;

            for (; moved < steps; moved++)
            {
                var next = _navigation.GetPosition(current, unit, forward ? 1 : -1);

                if (next.Equals(current))
                {
                    break;
                }

                current = next;
            }

            return (forward ? moved : -moved, current);
        }
    }
}
