using System;
using System.Collections.Generic;
using Avalonia.Automation.Peers;
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
            // Bound the read to maxLength source characters so a capped read of a large range does not
            // materialize the whole document; block breaks only add characters, so this still yields at
            // least maxLength of presentation text. Internal offset math elsewhere keeps using the flat
            // navigation text, so the Length == Δoffset invariant is unaffected.
            var end = _end;
            if (maxLength >= 0)
            {
                var capped = _navigation.GetPosition(_start, maxLength);
                if (capped.CompareTo(_end) < 0)
                    end = capped;
            }

            var range = _navigation.GetRange(_start, end);
            var text = _navigation is IAccessibleText accessible
                ? accessible.GetBlockSeparatedText(range)
                : _navigation.GetText(range);

            return maxLength >= 0 && text.Length > maxLength ? text.Substring(0, maxLength) : text;
        }

        public int Move(TextUnit unit, int count)
        {
            if (count == 0)
            {
                return 0;
            }

            // Normalize to the enclosing unit, then move whole units.
            ExpandToEnclosingUnit(unit);

            var forward = count > 0;
            var moved = 0;

            for (var target = Math.Abs(count); moved < target; moved++)
            {
                var startBefore = _start.Offset;
                var endBefore = _end.Offset;

                var stepped = forward ? MoveToNextUnit(unit, endBefore) : MoveToPreviousUnit(unit, startBefore);
                if (!stepped || (_start.Offset == startBefore && _end.Offset == endBefore))
                {
                    break;
                }
            }

            return forward ? moved : -moved;
        }

        // The first enclosing unit beginning at or after `from`. Walking past the end skips the boundary
        // positions that re-enclose to the current unit, and whitespace-only word segments (UIA word
        // moves do not stop on a run of spaces between words).
        private bool MoveToNextUnit(TextUnit unit, int from)
        {
            var probe = _end;
            while (true)
            {
                var enclosing = _navigation.GetRangeEnclosing(probe, unit);
                if (enclosing.Start.Offset >= from && enclosing.End.Offset > from)
                {
                    if (!IsWordGap(unit, enclosing))
                    {
                        _start = enclosing.Start;
                        _end = enclosing.End;
                        return true;
                    }

                    if (enclosing.End.Offset > probe.Offset)
                    {
                        probe = enclosing.End;
                        continue;
                    }
                }

                var next = _navigation.GetPosition(probe, 1);
                if (next.Offset == probe.Offset)
                {
                    return false; // document end
                }

                probe = next;
            }
        }

        private bool MoveToPreviousUnit(TextUnit unit, int from)
        {
            var probe = _start;
            while (true)
            {
                var previous = _navigation.GetPosition(probe, -1);
                if (previous.Offset == probe.Offset)
                {
                    return false; // document start
                }

                var enclosing = _navigation.GetRangeEnclosing(previous, unit);
                if (enclosing.Start.Offset < from && !IsWordGap(unit, enclosing))
                {
                    _start = enclosing.Start;
                    _end = enclosing.End;
                    return true;
                }

                probe = enclosing.Start.Offset < probe.Offset ? enclosing.Start : previous;
            }
        }

        private bool IsWordGap(TextUnit unit, ITextRange range)
            => unit == TextUnit.Word && string.IsNullOrWhiteSpace(_navigation.GetText(range));

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

        public void ScrollIntoView(bool alignToTop)
        {
            if (_navigation is IAccessibleText accessible)
            {
                accessible.ScrollIntoView(_navigation.GetRange(_start, _end));
            }
        }

        public Rect[] GetBoundingRectangles()
            => _navigation is IAccessibleText accessible
                ? accessible.GetBoundingRectangles(_navigation.GetRange(_start, _end))
                : Array.Empty<Rect>();

        public object? GetAttributeValue(TextAttribute attribute)
        {
            if (_navigation is not IAccessibleText accessible)
            {
                return null;
            }

            var (attributes, run) = accessible.GetTextAttributes(_start);

            // Report the value only when its run covers the whole range; otherwise it is mixed.
            if (run.Start.CompareTo(_start) > 0 || run.End.CompareTo(_end) < 0)
            {
                return null;
            }

            return attributes.TryGetValue(attribute, out var value) ? value : null;
        }

        public ITextRangeProvider? FindText(string text, bool backward, bool ignoreCase)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            var haystack = _navigation.GetText(_navigation.GetRange(_start, _end));
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            var index = backward
                ? haystack.LastIndexOf(text, comparison)
                : haystack.IndexOf(text, comparison);

            if (index < 0)
            {
                return null;
            }

            // The match offsets are relative to this range's start.
            return new AutomationTextRange(
                _navigation,
                _navigation.GetPosition(_start, index),
                _navigation.GetPosition(_start, index + text.Length));
        }

        public IReadOnlyList<AutomationPeer> GetChildren()
            => _navigation is ITextEmbeddedObjects embedded
                ? embedded.GetEmbeddedObjects(_navigation.GetRange(_start, _end))
                : Array.Empty<AutomationPeer>();

        public AutomationPeer? GetEnclosingElement()
            => _navigation is ITextEmbeddedObjects embedded
                ? embedded.GetEnclosingElement(_navigation.GetRange(_start, _end))
                : null;

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
