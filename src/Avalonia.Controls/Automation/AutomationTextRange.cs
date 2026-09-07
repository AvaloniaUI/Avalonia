using System;
using System.Collections.Generic;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Input.TextInput;
using Avalonia.Media.TextFormatting;

namespace Avalonia.Automation
{
    /// <summary>
    /// A mutable text-range cursor over an <see cref="IAccessibleText"/>, the range the platform
    /// accessibility backends (Win32 UIA TextPattern, AT-SPI) wrap for their protocol.
    /// </summary>
    /// <remarks>
    /// The endpoints are held as a tracked range, so a range a client keeps across edits goes on
    /// covering the same content. They take outward gravity: text typed at either edge joins the range,
    /// which keeps a document range covering the whole document. All text is read through
    /// <see cref="IAccessibleText.GetPlainText"/>.
    /// </remarks>
    internal sealed class AutomationTextRange : ITextRangeProvider
    {
        private readonly IAccessibleText _text;
        private ITrackedTextRange _range;

        public AutomationTextRange(IAccessibleText text, ITextPointer start, ITextPointer end)
        {
            _text = text ?? throw new ArgumentNullException(nameof(text));

            if (start is null)
            {
                throw new ArgumentNullException(nameof(start));
            }

            if (end is null)
            {
                throw new ArgumentNullException(nameof(end));
            }

            _range = start.CompareTo(end) <= 0 ? Track(start, end) : Track(end, start);
        }

        private ITextPointer Start => _range.Start;

        private ITextPointer End => _range.End;

        /// <inheritdoc />
        public ITextRangeProvider Clone() => new AutomationTextRange(_text, Start, End);

        /// <inheritdoc />
        public bool Compare(ITextRangeProvider other)
            => other is AutomationTextRange range && Start.Equals(range.Start) && End.Equals(range.End);

        /// <inheritdoc />
        public int CompareEndpoints(TextRangeEndpoint endpoint, ITextRangeProvider other, TextRangeEndpoint otherEndpoint)
        {
            var range = Own(other);
            return Endpoint(endpoint).CompareTo(range.Endpoint(otherEndpoint));
        }

        /// <inheritdoc />
        public void ExpandToEnclosingUnit(TextUnit unit)
        {
            var enclosing = _text.GetRangeEnclosing(Probe(Start), unit);
            SetRange(enclosing.Start, enclosing.End);
        }

        /// <inheritdoc />
        public string GetText(int maxLength)
        {
            // Bound the read by characters so a capped read of a large range does not materialize the
            // whole document. A character is at least one code unit and block separators only add
            // text, so the bounded read still yields at least maxLength characters when there are any.
            var end = End;
            if (maxLength >= 0)
            {
                var capped = _text.GetPosition(Start, TextUnit.Character, maxLength);
                if (capped.CompareTo(end) < 0)
                {
                    end = capped;
                }
            }

            var text = _text.GetPlainText(_text.GetRange(Start, end));
            return maxLength >= 0 && text.Length > maxLength ? text.Substring(0, maxLength) : text;
        }

        /// <inheritdoc />
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
                var startBefore = Start;
                var endBefore = End;

                var stepped = forward ? MoveToNextUnit(unit, endBefore) : MoveToPreviousUnit(unit, startBefore);
                if (!stepped || (Start.Equals(startBefore) && End.Equals(endBefore)))
                {
                    break;
                }
            }

            return forward ? moved : -moved;
        }

        // The first enclosing unit beginning at or after `from`. Walking past the end skips the boundary
        // positions that re-enclose to the current unit, and whitespace-only word segments (UIA word
        // moves do not stop on a run of spaces between words).
        private bool MoveToNextUnit(TextUnit unit, ITextPointer from)
        {
            var probe = End;
            while (true)
            {
                var enclosing = _text.GetRangeEnclosing(Probe(probe), unit);
                if (enclosing.Start.CompareTo(from) >= 0 && enclosing.End.CompareTo(from) > 0)
                {
                    if (!IsWordGap(unit, enclosing))
                    {
                        SetRange(enclosing.Start, enclosing.End);
                        return true;
                    }

                    if (enclosing.End.CompareTo(probe) > 0)
                    {
                        probe = enclosing.End;
                        continue;
                    }
                }

                var next = _text.GetPosition(probe, TextUnit.Character, 1);
                if (next.Equals(probe))
                {
                    return false; // document end
                }

                probe = next;
            }
        }

        private bool MoveToPreviousUnit(TextUnit unit, ITextPointer from)
        {
            var probe = Start;
            while (true)
            {
                var previous = _text.GetPosition(probe, TextUnit.Character, -1);
                if (previous.Equals(probe))
                {
                    return false; // document start
                }

                var enclosing = _text.GetRangeEnclosing(Probe(previous), unit);
                if (enclosing.Start.CompareTo(from) < 0 && !IsWordGap(unit, enclosing))
                {
                    SetRange(enclosing.Start, enclosing.End);
                    return true;
                }

                probe = enclosing.Start.CompareTo(probe) < 0 ? enclosing.Start : previous;
            }
        }

        private bool IsWordGap(TextUnit unit, ITextRange range)
            => unit == TextUnit.Word && string.IsNullOrWhiteSpace(_text.GetPlainText(range));

        /// <inheritdoc />
        public int MoveEndpointByUnit(TextRangeEndpoint endpoint, TextUnit unit, int count)
        {
            var (moved, position) = StepByUnit(Endpoint(endpoint), unit, count);

            SetEndpoint(endpoint, position);

            return moved;
        }

        /// <inheritdoc />
        public void MoveEndpointByRange(TextRangeEndpoint endpoint, ITextRangeProvider other, TextRangeEndpoint otherEndpoint)
        {
            var range = Own(other);
            SetEndpoint(endpoint, range.Endpoint(otherEndpoint));
        }

        /// <inheritdoc />
        public void Select() => _text.SetSelection(new DirectedTextRange(Start, End));

        /// <inheritdoc />
        public void ScrollIntoView(bool alignToTop) => _text.ScrollIntoView(_text.GetRange(Start, End));

        /// <inheritdoc />
        public Rect[] GetBoundingRectangles() => _text.GetBoundingRectangles(_text.GetRange(Start, End));

        /// <inheritdoc />
        public object? GetAttributeValue(TextAttribute attribute)
        {
            var start = Start;
            var end = End;
            var (attributes, run) = _text.GetTextAttributes(start);

            // Report the value only when its run covers the whole range; otherwise it is mixed.
            if (run.Start.CompareTo(start) > 0 || run.End.CompareTo(end) < 0)
            {
                return null;
            }

            return attributes.TryGetValue(attribute, out var value) ? value : null;
        }

        /// <inheritdoc />
        public ITextRangeProvider? FindText(string text, bool backward, bool ignoreCase)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            var start = Start;
            var end = End;
            ITextRange? match = null;

            // The plain text inside one scope lines up with that scope's offsets, so a match is mapped
            // back scope by scope. A match spanning two blocks is not found.
            var position = start;
            while (true)
            {
                var scope = _text.GetLinearScope(position);
                var from = Max(start, scope.Range.Start);
                var to = Min(end, scope.Range.End);

                if (from.CompareTo(to) < 0)
                {
                    var haystack = _text.GetPlainText(_text.GetRange(from, to));
                    var index = backward ? haystack.LastIndexOf(text, comparison) : haystack.IndexOf(text, comparison);

                    if (index >= 0)
                    {
                        match = scope.GetRange(scope.GetOffset(from) + index, text.Length);

                        if (!backward)
                        {
                            break;
                        }
                    }
                }

                if (scope.Range.End.CompareTo(end) >= 0)
                {
                    break;
                }

                var next = _text.GetPosition(scope.Range.End, TextUnit.Character, 1);
                if (next.CompareTo(scope.Range.End) <= 0)
                {
                    break;
                }

                position = next;
            }

            return match is null ? null : new AutomationTextRange(_text, match.Start, match.End);
        }

        /// <inheritdoc />
        public IReadOnlyList<AutomationPeer> GetChildren()
            => _text is ITextEmbeddedObjects embedded
                ? embedded.GetEmbeddedObjects(_text.GetRange(Start, End))
                : Array.Empty<AutomationPeer>();

        /// <inheritdoc />
        public AutomationPeer? GetEnclosingElement()
            => _text is ITextEmbeddedObjects embedded
                ? embedded.GetEnclosingElement(_text.GetRange(Start, End))
                : null;

        private ITextPointer Endpoint(TextRangeEndpoint endpoint) => endpoint == TextRangeEndpoint.Start ? Start : End;

        // UIA ranges carry no gravity, so enclosing queries use forward probes: a boundary tie always
        // resolves to the following unit, whichever endpoint the probe came from.
        private ITextPointer Probe(ITextPointer position) => _text.WithGravity(position, LogicalDirection.Forward);

        private ITrackedTextRange Track(ITextPointer start, ITextPointer end)
            => _text.TrackRange(_text.GetRange(
                _text.WithGravity(start, LogicalDirection.Backward),
                _text.WithGravity(end, LogicalDirection.Forward)));

        private void SetRange(ITextPointer start, ITextPointer end)
        {
            var previous = _range;
            _range = Track(start, end);
            previous.Dispose();
        }

        // Moves an endpoint; if it passes the other endpoint the range collapses (UIA semantics).
        private void SetEndpoint(TextRangeEndpoint endpoint, ITextPointer position)
        {
            var start = Start;
            var end = End;

            if (endpoint == TextRangeEndpoint.Start)
            {
                start = position;
                if (start.CompareTo(end) > 0)
                {
                    end = start;
                }
            }
            else
            {
                end = position;
                if (end.CompareTo(start) < 0)
                {
                    start = end;
                }
            }

            SetRange(start, end);
        }

        private (int Moved, ITextPointer Position) StepByUnit(ITextPointer origin, TextUnit unit, int count)
        {
            var forward = count >= 0;
            var steps = Math.Abs(count);
            var current = origin;
            var moved = 0;

            for (; moved < steps; moved++)
            {
                var next = _text.GetPosition(current, unit, forward ? 1 : -1);

                if (next.Equals(current))
                {
                    break;
                }

                current = next;
            }

            return (forward ? moved : -moved, current);
        }

        private AutomationTextRange Own(ITextRangeProvider other)
            => other is AutomationTextRange range && ReferenceEquals(range._text, _text)
                ? range
                : throw new ArgumentException("The range was not produced by this provider.", nameof(other));

        private static ITextPointer Max(ITextPointer a, ITextPointer b) => a.CompareTo(b) >= 0 ? a : b;

        private static ITextPointer Min(ITextPointer a, ITextPointer b) => a.CompareTo(b) <= 0 ? a : b;
    }
}
