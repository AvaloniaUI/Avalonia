using System;
using System.Collections.Generic;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Input.TextInput;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Media.TextFormatting.Unicode;

namespace Avalonia.Controls
{
    /// <summary>
    /// An <see cref="ITextNavigation"/> over a <see cref="TextBox"/>'s text, for the automation peer
    /// and the accessibility text providers. Unit boundaries come from <c>TextSegmentation</c>, so a
    /// second navigation over the same control reports the same units.
    /// </summary>
    /// <remarks>
    /// A TextBox is one block of plain text, so its single linear scope is the whole document and a
    /// pointer's position is its offset into <see cref="TextBox.Text"/>.
    /// </remarks>
    internal sealed class TextBoxTextNavigation : IAccessibleText
    {
        private readonly TextBox _textBox;
        private readonly DocumentScope _scope;
        // Weak, so a range a consumer drops without disposing (an automation range a client releases
        // through COM, say) stops costing work once it is collected.
        private readonly List<WeakReference<TrackedRange>> _trackedRanges = new();
        private long _version;
        private EventHandler<TextChange>? _textChanged;
        private (int Anchor, int Active, int Caret) _reportedSelection;

        public TextBoxTextNavigation(TextBox textBox)
        {
            _textBox = textBox ?? throw new ArgumentNullException(nameof(textBox));
            _scope = new DocumentScope(this);
            _reportedSelection = CurrentSelection();
            _textBox.PropertyChanged += OnTextBoxPropertyChanged;
            _textBox.SelectionSettled += OnSelectionSettled;
        }

        public ITextPointer DocumentStart => CreatePointer(0);

        public ITextPointer DocumentEnd => CreatePointer(Text.Length, LogicalDirection.Backward);

        public ITextRange DocumentRange => new NavRange(CreatePointer(0), CreatePointer(Text.Length));

        public long DocumentVersion => _version;

        public event EventHandler<TextChange>? TextChanged
        {
            add => _textChanged += value;
            remove => _textChanged -= value;
        }

        public event EventHandler? SelectionChanged;

        public event EventHandler<TextChange>? PlainTextChanged;

        private string Text => _textBox.Text ?? string.Empty;

        // Units are found in the text as drawn: segmenting the real text of a masked password would
        // reveal its word and sentence structure through the unit boundaries.
        private string SegmentationText =>
            TextBoxAutomationPeer.GetDisplayedText(Text, _textBox.PasswordChar, _textBox.RevealPassword)
            ?? string.Empty;

        public ITextPointer GetPosition(ITextPointer origin, TextUnit unit, int count)
        {
            var offset = OffsetOf(origin);

            if (count == 0)
            {
                return CreatePointer(offset, origin.Gravity);
            }

            var text = SegmentationText;
            var forward = count > 0;
            var current = offset;

            for (var i = 0; i < Math.Abs(count); i++)
            {
                var next = TextSegmentation.MoveByUnit(current, unit, forward, text);
                if (next == current)
                {
                    break;
                }

                current = next;
            }

            return CreatePointer(current, forward ? LogicalDirection.Forward : LogicalDirection.Backward);
        }

        public ITextPointer WithGravity(ITextPointer position, LogicalDirection gravity)
            => CreatePointer(OffsetOf(position), gravity);

        public ITextRange GetRangeEnclosing(ITextPointer position, TextUnit unit)
        {
            var text = SegmentationText;
            var offset = OffsetOf(position);

            switch (unit)
            {
                case TextUnit.Document:
                case TextUnit.Page:
                case TextUnit.Format:
                    return new NavRange(CreatePointer(0), CreatePointer(text.Length));

                default:
                    var (start, end) = TextSegmentation.UnitBounds(offset, unit, position.Gravity, text);
                    return new NavRange(CreatePointer(start), CreatePointer(end));
            }
        }

        public ITextRange GetRange(ITextPointer a, ITextPointer b)
        {
            var oa = OffsetOf(a);
            var ob = OffsetOf(b);

            return oa <= ob
                ? new NavRange(CreatePointer(oa, a.Gravity), CreatePointer(ob, b.Gravity))
                : new NavRange(CreatePointer(ob, b.Gravity), CreatePointer(oa, a.Gravity));
        }

        public ITrackedTextRange TrackRange(ITextRange range)
        {
            var (start, end) = Span(range);
            var tracked = new TrackedRange(this, start, range.Start.Gravity, end, range.End.Gravity);
            _trackedRanges.Add(new WeakReference<TrackedRange>(tracked));
            return tracked;
        }

        public string GetText(ITextRange range) => Slice(Text, range);

        public string GetPlainText(ITextRange range) => Slice(SegmentationText, range);

        public ILinearTextScope GetLinearScope(ITextPointer position)
        {
            _ = OffsetOf(position);
            return _scope;
        }

        public void SetSelection(DirectedTextRange selection)
        {
            var anchor = OffsetOf(selection.Anchor);
            var active = OffsetOf(selection.Active);

            // A non-empty selection does not move CaretIndex, and setting CaretIndex collapses the
            // selection, so the caret goes to the active end first. SelectionEnd is the end the TextBox
            // extends from the keyboard, so it takes the active end. The change scope keeps the
            // collapsed state in between from being reported.
            using var _ = _textBox.BeginChange();
            _textBox.CaretIndex = active;
            _textBox.SelectionStart = anchor;
            _textBox.SelectionEnd = active;
        }

        public DirectedTextRange GetSelection()
            => new(CreatePointer(_textBox.SelectionStart), CreatePointer(_textBox.SelectionEnd));

        public ITextRange GetCaret()
        {
            var caret = CreatePointer(_textBox.CaretIndex);
            return new NavRange(caret, caret);
        }

        public Rect[] GetBoundingRectangles(ITextRange range)
        {
            var (start, end) = Span(range);
            return _textBox.GetTextRangeBounds(start, end - start);
        }

        public ITextPointer? GetPositionFromPoint(Point point)
        {
            var offset = _textBox.GetOffsetFromTopLevelPoint(point);
            return offset < 0 ? null : CreatePointer(offset);
        }

        public ITextRange? GetVisibleRange()
        {
            var (start, end) = _textBox.GetVisibleTextRange();
            return start < 0 ? null : new NavRange(CreatePointer(start), CreatePointer(end));
        }

        public void ScrollIntoView(ITextRange range)
        {
            var (start, end) = Span(range);
            _textBox.ScrollTextRangeIntoView(start, end - start);
        }

        public (IReadOnlyDictionary<TextAttribute, object?> Attributes, ITextRange Run) GetTextAttributes(ITextPointer position)
        {
            // Validate the pointer is ours; a TextBox is uniform, so the offset itself is unused.
            _ = OffsetOf(position);

            var attributes = new Dictionary<TextAttribute, object?>
            {
                [TextAttribute.FontFamily] = _textBox.FontFamily?.Name,
                [TextAttribute.FontSize] = _textBox.FontSize,
                [TextAttribute.FontWeight] = _textBox.FontWeight,
                [TextAttribute.FontStyle] = _textBox.FontStyle,
                [TextAttribute.IsReadOnly] = _textBox.IsReadOnly,
            };

            if ((_textBox.Foreground as ISolidColorBrush)?.Color is { } foreground)
            {
                attributes[TextAttribute.Foreground] = foreground;
            }

            if ((_textBox.Background as ISolidColorBrush)?.Color is { } background)
            {
                attributes[TextAttribute.Background] = background;
            }

            return (attributes, DocumentRange);
        }

        private (int Anchor, int Active, int Caret) CurrentSelection()
            => (_textBox.SelectionStart, _textBox.SelectionEnd, _textBox.CaretIndex);

        private void OnSelectionSettled(object? sender, EventArgs e)
        {
            var current = CurrentSelection();
            if (current == _reportedSelection)
            {
                return;
            }

            _reportedSelection = current;
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnTextBoxPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == TextBox.PasswordCharProperty || e.Property == TextBox.RevealPasswordProperty)
            {
                OnMaskingChanged(e);
                return;
            }

            if (e.Property != TextBox.TextProperty)
            {
                return;
            }

            var (offset, oldLength, newLength) =
                TextSegmentation.ComputeChange(e.OldValue as string ?? string.Empty, e.NewValue as string ?? string.Empty);

            if (oldLength == 0 && newLength == 0)
            {
                return;
            }

            _version++;

            for (var i = _trackedRanges.Count - 1; i >= 0; i--)
            {
                if (_trackedRanges[i].TryGetTarget(out var tracked))
                {
                    tracked.Apply(offset, oldLength, newLength);
                }
                else
                {
                    _trackedRanges.RemoveAt(i);
                }
            }

            var change = new TextChange(new NavRange(CreatePointer(offset), CreatePointer(offset + newLength)));
            _textChanged?.Invoke(this, change);
            PlainTextChanged?.Invoke(this, change);
            _scope.RaiseTextChanged(new LinearTextChange(offset, oldLength, newLength));
        }

        // Masking alters every drawn character at once, so the whole document is reported as replaced.
        private void OnMaskingChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var passwordChar = _textBox.PasswordChar;
            var reveal = _textBox.RevealPassword;
            var oldPasswordChar = e.Property == TextBox.PasswordCharProperty ? e.GetOldValue<char>() : passwordChar;
            var oldReveal = e.Property == TextBox.RevealPasswordProperty ? e.GetOldValue<bool>() : reveal;

            if (Text.Length == 0 || DrawnMask(oldPasswordChar, oldReveal) == DrawnMask(passwordChar, reveal))
            {
                return;
            }

            PlainTextChanged?.Invoke(this, new TextChange(DocumentRange));

            static char DrawnMask(char passwordChar, bool reveal) => reveal ? default : passwordChar;
        }

        private string Slice(string text, ITextRange range)
        {
            var (start, end) = Span(range);
            return text.Substring(start, end - start);
        }

        private (int Start, int End) Span(ITextRange range)
        {
            var start = OffsetOf(range.Start);
            var end = OffsetOf(range.End);
            return start <= end ? (start, end) : (end, start);
        }

        private ITextPointer CreatePointer(int offset, LogicalDirection gravity = LogicalDirection.Forward) =>
            new NavPointer(this, Math.Clamp(offset, 0, Text.Length), gravity);

        private int OffsetOf(ITextPointer pointer)
        {
            if (pointer is not NavPointer own || own.Owner != this)
            {
                throw new ArgumentException("The text pointer was not produced by this navigator.", nameof(pointer));
            }

            return Math.Clamp(own.Offset, 0, Text.Length);
        }

        private sealed class DocumentScope : ILinearTextScope
        {
            private readonly TextBoxTextNavigation _owner;

            public DocumentScope(TextBoxTextNavigation owner) => _owner = owner;

            public ITextRange Range => _owner.DocumentRange;

            public int Length => _owner.Text.Length;

            public event EventHandler<LinearTextChange>? TextChanged;

            // The whole text is one block, so no edit can dissolve the scope.
            public bool IsValid => true;

            public event EventHandler? Invalidated
            {
                add { }
                remove { }
            }

            public int GetOffset(ITextPointer position) => _owner.OffsetOf(position);

            public ITextPointer GetPosition(int offset, LogicalDirection gravity)
            {
                var text = _owner.Text;
                var target = Math.Clamp(offset, 0, text.Length);
                return _owner.CreatePointer(
                    TextSegmentation.SnapToValid(target, text, gravity == LogicalDirection.Forward),
                    gravity);
            }

            public ITextRange GetRange(int start, int length)
                => new NavRange(
                    GetPosition(start, LogicalDirection.Forward),
                    GetPosition(start + Math.Max(0, length), LogicalDirection.Backward));

            public string GetText(int start, int length)
            {
                var text = _owner.Text;
                var from = Math.Clamp(start, 0, text.Length);
                var to = Math.Clamp(start + Math.Max(0, length), from, text.Length);
                return text.Substring(from, to - from);
            }

            internal void RaiseTextChanged(LinearTextChange change) => TextChanged?.Invoke(this, change);
        }

        internal int TrackedRangeCount => _trackedRanges.Count;

        private sealed class TrackedRange : ITrackedTextRange
        {
            private readonly TextBoxTextNavigation _owner;
            private readonly LogicalDirection _startGravity;
            private readonly LogicalDirection _endGravity;
            private int _start;
            private int _end;
            private bool _disposed;

            public TrackedRange(
                TextBoxTextNavigation owner,
                int start,
                LogicalDirection startGravity,
                int end,
                LogicalDirection endGravity)
            {
                _owner = owner;
                _start = start;
                _startGravity = startGravity;
                _end = end;
                _endGravity = endGravity;
            }

            public ITextPointer Start => _owner.CreatePointer(_start, _startGravity);

            public ITextPointer End => _owner.CreatePointer(_end, _endGravity);

            public bool IsEmpty => _start == _end;

            // The change replaced oldLength code units at offset with newLength new ones.
            public void Apply(int offset, int oldLength, int newLength)
            {
                _start = Track(_start, _startGravity, offset, oldLength, newLength);
                _end = Track(_end, _endGravity, offset, oldLength, newLength);

                if (_end < _start)
                {
                    _end = _start;
                }
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _owner._trackedRanges.RemoveAll(r => !r.TryGetTarget(out var target) || target == this);
            }

            private static int Track(int position, LogicalDirection gravity, int offset, int oldLength, int newLength)
            {
                var removedEnd = offset + oldLength;

                if (position > removedEnd)
                {
                    return position - oldLength + newLength;
                }

                if (position < offset)
                {
                    return position;
                }

                if (oldLength > 0)
                {
                    // Replaced content keeps the endpoints at its edges on their side, so a range that
                    // covered the old content covers its replacement.
                    if (position == offset)
                    {
                        return offset;
                    }

                    if (position == removedEnd)
                    {
                        return offset + newLength;
                    }
                }

                // An insertion exactly at the endpoint, or an endpoint inside removed content: gravity
                // picks the side of the new content.
                return gravity == LogicalDirection.Forward ? offset + newLength : offset;
            }
        }

        private sealed class NavPointer : ITextPointer
        {
            public NavPointer(TextBoxTextNavigation owner, int offset, LogicalDirection direction)
            {
                Owner = owner;
                Offset = offset;
                Gravity = direction;
            }

            public TextBoxTextNavigation Owner { get; }

            public int Offset { get; }

            public LogicalDirection Gravity { get; }

            public int CompareTo(ITextPointer? other)
            {
                if (other is null)
                {
                    return 1;
                }

                return Offset.CompareTo(Owner.OffsetOf(other));
            }

            public bool Equals(ITextPointer? other) =>
                other is NavPointer own && own.Owner == Owner && own.Offset == Offset;

            public override bool Equals(object? obj) => obj is ITextPointer other && Equals(other);

            public override int GetHashCode() => Offset;
        }

        private sealed class NavRange : ITextRange
        {
            public NavRange(ITextPointer start, ITextPointer end)
            {
                Start = start;
                End = end;
            }

            public ITextPointer Start { get; }

            public ITextPointer End { get; }

            public bool IsEmpty => Start.Equals(End);
        }
    }
}
