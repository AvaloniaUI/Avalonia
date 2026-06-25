using System;
using System.Collections.Generic;
using Avalonia.Controls.Presenters;
using Avalonia.Input.TextInput;
using Avalonia.Media.TextFormatting;
using Avalonia.Reactive;
using Avalonia.Utilities;

namespace Avalonia.Controls
{
    internal class TextBoxTextInputMethodClient : TextInputMethodClient, IStructuredTextInput, ITextNavigation
    {
        private TextBox? _parent;
        private TextPresenter? _presenter;
        private bool _selectionChanged;
        private bool _textChanged;
        private bool _isInChange;
        private EventHandler? _caretBoundsChangedHandler;
        private ITextRange? _compositionRange;
        private long _documentVersion;
        private EventHandler<TextChange>? _navTextChanged;

        public event EventHandler? TextChanged;
        public event EventHandler? CaretPositionChanged;
        public event EventHandler? CompositionChanged;

        public override Visual TextViewVisual => _presenter!;

        public override string SurroundingText
        {
            get
            {
                if (_presenter is null || _parent is null)
                {
                    return "";
                }

                if (_parent.CaretIndex != _presenter.CaretIndex)
                {
                    _presenter.SetCurrentValue(TextPresenter.CaretIndexProperty, _parent.CaretIndex);
                }

                if (_parent.Text != _presenter.Text)
                {
                    _presenter.SetCurrentValue(TextPresenter.TextProperty, _parent.Text);
                }

                var lineIndex = _presenter.TextLayout.GetLineIndexFromCharacterIndex(_presenter.CaretIndex, false);

                var textLine = _presenter.TextLayout.TextLines[lineIndex];

                var lineText = GetTextLineText(textLine);

                return lineText;
            }
        }

        public override Rect CursorRectangle
        {
            get
            {
                if (_parent == null || _presenter == null)
                {
                    return default;
                }

                var transform = _presenter.TransformToVisual(_parent);

                if (transform == null)
                {
                    return default;
                }

                return _presenter.GetCursorRectangle().TransformToAABB(transform.Value);
            }
        }

        public override TextSelection Selection
        {
            get
            {
                if (_presenter is null || _parent is null)
                {
                    return default;
                }

                var lineIndex = _presenter.TextLayout.GetLineIndexFromCharacterIndex(_parent.CaretIndex, false);

                var textLine = _presenter.TextLayout.TextLines[lineIndex];

                var lineStart = textLine.FirstTextSourceIndex;

                var selectionStart = Math.Max(0, _parent.SelectionStart - lineStart);

                var selectionEnd = Math.Max(0, _parent.SelectionEnd - lineStart);

                return new TextSelection(selectionStart, selectionEnd);
            }
            set
            {
                if (_parent is null || _presenter is null)
                {
                    return;
                }

                var lineIndex = _presenter.TextLayout.GetLineIndexFromCharacterIndex(_parent.CaretIndex, false);

                var textLine = _presenter.TextLayout.TextLines[lineIndex];

                var lineStart = textLine.FirstTextSourceIndex;

                var selectionStart = lineStart + value.Start;
                var selectionEnd = lineStart + value.End;

                _parent.SelectionStart = selectionStart;
                _parent.SelectionEnd = selectionEnd;

                RaiseSelectionChanged();
            }
        }

        public override bool SupportsPreedit => true;

        public override bool SupportsSurroundingText => true;

        public void SetPresenter(TextPresenter? presenter, TextBox? parent)
        {
            if (_parent != null)
            {
                _parent.PropertyChanged -= OnParentPropertyChanged;
                _parent.Tapped -= OnParentTapped;
            }

            _parent = parent;

            if (_parent != null)
            {
                _parent.PropertyChanged += OnParentPropertyChanged;
                _parent.Tapped += OnParentTapped;
            }

            var oldPresenter = _presenter;

            if (oldPresenter != null)
            {
                oldPresenter.CurrentImClient = null;
                oldPresenter.ClearValue(TextPresenter.PreeditTextProperty);

                if (_caretBoundsChangedHandler != null)
                {
                    oldPresenter.CaretBoundsChanged -= _caretBoundsChangedHandler;
                }
            }

            _presenter = presenter;

            if (_presenter != null)
            {
                _presenter.CurrentImClient = this;
                _caretBoundsChangedHandler ??= OnPresenterCaretBoundsChanged;
                _presenter.CaretBoundsChanged += _caretBoundsChangedHandler;
            }

            RaiseTextViewVisualChanged();

            RaiseCursorRectangleChanged();
        }

        private void OnPresenterCaretBoundsChanged(object? sender, EventArgs e)
        {
            RaiseCursorRectangleChanged();
            RaiseCaretPositionChangedCore();
        }

        private void OnParentTapped(object? sender, Input.TappedEventArgs e)
        {
            RaiseInputPaneActivationRequested();
        }

        public override void SetPreeditText(string? preeditText) => SetPreeditText(preeditText, null);

        public override void SetPreeditText(string? preeditText, int? cursorPos)
        {
            if (_presenter == null || _parent == null)
            {
                return;
            }

            _presenter.SetCurrentValue(TextPresenter.PreeditTextProperty, preeditText);
            _presenter.SetCurrentValue(TextPresenter.PreeditTextCursorPositionProperty, cursorPos);
        }

        ITextPointer IStructuredTextInput.CaretPosition =>
            CreateLocalPointer(_parent?.CaretIndex ?? 0, LogicalDirection.Forward);

        ITextRange IStructuredTextInput.Selection
        {
            get
            {
                if (_parent is null)
                {
                    return CreateLocalRange(0, 0);
                }

                var start = Math.Min(_parent.SelectionStart, _parent.SelectionEnd);
                var end = Math.Max(_parent.SelectionStart, _parent.SelectionEnd);

                return CreateLocalRange(start, end);
            }
            set
            {
                if (_parent is null)
                {
                    return;
                }

                var (start, end) = GetAbsoluteRange(value);
                _parent.SelectionStart = start;
                _parent.SelectionEnd = end;
            }
        }

        ITextRange? IStructuredTextInput.CompositionRange
        {
            get => _compositionRange;
            set => SetCompositionRangeCore(value, raiseEvent: true);
        }

        ITextPointer IStructuredTextInput.CreatePointer(int offset, LogicalDirection direction)
            => CreateLocalPointer(offset, direction);

        ITextRange IStructuredTextInput.CreateRange(ITextPointer start, ITextPointer end)
            => CreateLocalRange(GetAbsoluteOffset(start), GetAbsoluteOffset(end));

        void IStructuredTextInput.ReplaceText(ITextRange range, string text)
        {
            if (_parent is null)
            {
                return;
            }

            using var _ = BeginChange();

            var (start, end) = GetAbsoluteRange(range);

            ReplaceTextCore(start, end, text, clearComposition: true);
        }

        void IStructuredTextInput.SetCompositionText(string? text, int cursorOffset)
        {
            if (_parent is null)
            {
                return;
            }

            using var _ = BeginChange();

            if (text is null)
            {
                if (_compositionRange is { } activeRange)
                {
                    var (activeStart, activeEnd) = GetAbsoluteRange(activeRange);
                    ReplaceTextCore(activeStart, activeEnd, string.Empty, clearComposition: false);
                }

                SetCompositionRangeCore(null, raiseEvent: true);
                return;
            }

            int start;
            if (_compositionRange is { } existing)
            {
                var (existingStart, existingEnd) = GetAbsoluteRange(existing);
                start = existingStart;
                ReplaceTextCore(existingStart, existingEnd, text, clearComposition: false);
            }
            else
            {
                var selectionStart = Math.Min(_parent.SelectionStart, _parent.SelectionEnd);
                var selectionEnd = Math.Max(_parent.SelectionStart, _parent.SelectionEnd);
                start = selectionStart;
                ReplaceTextCore(selectionStart, selectionEnd, text, clearComposition: false);
            }

            var compositionEnd = start + text.Length;
            SetCompositionRangeCore(CreateLocalRange(start, compositionEnd), raiseEvent: true);

            var relativeCursor = Math.Clamp(cursorOffset, 0, text.Length);
            var caretOffset = start + relativeCursor;
            _parent.SelectionStart = caretOffset;
            _parent.SelectionEnd = caretOffset;
        }

        void IStructuredTextInput.CommitComposition()
        {
            SetCompositionRangeCore(null, raiseEvent: true);
        }

        Rect IStructuredTextInput.GetFirstRectForRange(ITextRange range)
        {
            if (_presenter is null)
            {
                return default;
            }

            var (start, end) = GetAbsoluteRange(range);
            if (start == end)
            {
                return ((IStructuredTextInput)this).GetCaretRect(CreateLocalPointer(start, LogicalDirection.Forward));
            }

            foreach (var rect in _presenter.TextLayout.HitTestTextRange(start, end - start))
            {
                return TransformPresenterRect(rect);
            }

            return default;
        }

        Rect IStructuredTextInput.GetCaretRect(ITextPointer position)
        {
            if (_presenter is null)
            {
                return default;
            }

            var offset = GetAbsoluteOffset(position);
            var rect = _presenter.TextLayout.HitTestTextPosition(offset);

            return TransformPresenterRect(rect);
        }

        Rect[] IStructuredTextInput.GetSelectionRects(ITextRange range)
        {
            if (_presenter is null)
            {
                return Array.Empty<Rect>();
            }

            var (start, end) = GetAbsoluteRange(range);
            if (start == end)
            {
                return Array.Empty<Rect>();
            }

            var result = new List<Rect>();
            foreach (var rect in _presenter.TextLayout.HitTestTextRange(start, end - start))
            {
                result.Add(TransformPresenterRect(rect));
            }

            return result.ToArray();
        }

        ITextPointer? IStructuredTextInput.GetClosestPosition(Point point)
        {
            if (_presenter is null)
            {
                return null;
            }

            var localPoint = TransformPointToPresenter(point);
            var hit = _presenter.TextLayout.HitTestPoint(localPoint);

            return CreateLocalPointer(hit.TextPosition, hit.IsTrailing ? LogicalDirection.Backward : LogicalDirection.Forward);
        }

        ITextPointer? IStructuredTextInput.GetClosestPosition(Point point, ITextRange withinRange)
        {
            var closest = ((IStructuredTextInput)this).GetClosestPosition(point);
            if (closest is null)
            {
                return null;
            }

            var (start, end) = GetAbsoluteRange(withinRange);
            var clamped = Math.Clamp(closest.Offset, start, end);

            return CreateLocalPointer(clamped, closest.Gravity);
        }

        ITextRange? IStructuredTextInput.GetCharacterRangeAtPoint(Point point)
        {
            var closest = ((IStructuredTextInput)this).GetClosestPosition(point);
            if (closest is null)
            {
                return null;
            }

            var start = closest.Offset;
            var end = Math.Min(GetDocumentLength(), start + 1);
            return CreateLocalRange(start, end);
        }

        ITextPointer? IStructuredTextInput.GetBoundaryPosition(ITextPointer position, TextUnit granularity,
            LogicalDirection direction)
        {
            var offset = GetAbsoluteOffset(position);
            var length = GetDocumentLength();

            if (granularity == TextUnit.Document)
            {
                return direction == LogicalDirection.Forward
                    ? CreateLocalPointer(length, LogicalDirection.Forward)
                    : CreateLocalPointer(0, LogicalDirection.Backward);
            }

            if (granularity == TextUnit.Character)
            {
                var characterBoundary = direction == LogicalDirection.Forward
                    ? Math.Min(length, offset + 1)
                    : Math.Max(0, offset - 1);

                return CreateLocalPointer(characterBoundary, direction);
            }

            var enclosing = ((ITextNavigation)this).GetRangeEnclosing(position, granularity);

            return direction == LogicalDirection.Forward
                ? CreateLocalPointer(enclosing.End.Offset, LogicalDirection.Forward)
                : CreateLocalPointer(enclosing.Start.Offset, LogicalDirection.Backward);
        }

        // ── ITextNavigation ────────────────────────────────────────────────

        ITextPointer ITextNavigation.DocumentStart => CreateLocalPointer(0, LogicalDirection.Forward);

        ITextPointer ITextNavigation.DocumentEnd =>
            CreateLocalPointer(GetDocumentLength(), LogicalDirection.Backward);

        ITextRange ITextNavigation.DocumentRange => CreateLocalRange(0, GetDocumentLength());

        long ITextNavigation.DocumentVersion => _documentVersion;

        ITextPointer ITextNavigation.GetPosition(ITextPointer origin, int distance)
        {
            var text = GetDocumentText();
            var target = Math.Clamp(RequireOwnOffset(origin) + distance, 0, text.Length);
            target = TextSegmentation.SnapToValid(target, text, forward: distance >= 0);

            return CreateLocalPointer(
                target,
                distance >= 0 ? LogicalDirection.Forward : LogicalDirection.Backward);
        }

        ITextPointer ITextNavigation.GetPosition(ITextPointer origin, TextUnit unit, int count)
        {
            var offset = RequireOwnOffset(origin);

            if (count == 0)
            {
                return CreateLocalPointer(offset, origin.Gravity);
            }

            var text = GetDocumentText();
            var forward = count > 0;
            var steps = Math.Abs(count);
            var current = offset;

            for (var i = 0; i < steps; i++)
            {
                var next = TextSegmentation.MoveByUnit(current, unit, forward, text);
                if (next == current)
                {
                    break;
                }

                current = next;
            }

            return CreateLocalPointer(current, forward ? LogicalDirection.Forward : LogicalDirection.Backward);
        }

        ITextRange ITextNavigation.GetRangeEnclosing(ITextPointer position, TextUnit unit)
        {
            var offset = RequireOwnOffset(position);
            var text = GetDocumentText();

            switch (unit)
            {
                case TextUnit.Document:
                case TextUnit.Page:
                case TextUnit.Format:
                    return CreateLocalRange(0, text.Length);

                case TextUnit.Character:
                    if (text.Length == 0)
                    {
                        return CreateLocalRange(0, 0);
                    }

                    var characterStart = Math.Clamp(offset, 0, text.Length - 1);
                    return CreateLocalRange(characterStart, TextSegmentation.NextGrapheme(characterStart, text));

                case TextUnit.Word:
                    return GetWordRange(offset, text);

                case TextUnit.Sentence:
                    return GetSentenceRange(offset, text);

                case TextUnit.Line:
                case TextUnit.Paragraph:
                    return GetLineRange(offset, text);

                default:
                    return CreateLocalRange(offset, offset);
            }
        }

        ITextRange ITextNavigation.GetRange(ITextPointer a, ITextPointer b)
        {
            var oa = RequireOwnOffset(a);
            var ob = RequireOwnOffset(b);

            return oa <= ob ? CreateLocalRange(oa, ob) : CreateLocalRange(ob, oa);
        }

        int ITextNavigation.GetOffset(ITextPointer from, ITextPointer to)
            => RequireOwnOffset(to) - RequireOwnOffset(from);

        string ITextNavigation.GetText(ITextRange range)
        {
            var text = GetDocumentText();
            var start = RequireOwnOffset(range.Start);
            var end = RequireOwnOffset(range.End);
            if (start > end)
            {
                (start, end) = (end, start);
            }

            return text.Substring(start, end - start);
        }

        event EventHandler<TextChange>? ITextNavigation.TextChanged
        {
            add => _navTextChanged += value;
            remove => _navTextChanged -= value;
        }

        private int RequireOwnOffset(ITextPointer pointer)
        {
            if (pointer is not SimpleTextPointer simple || simple.Owner != this)
            {
                throw new ArgumentException("The text pointer was not produced by this navigator.", nameof(pointer));
            }

            return Math.Clamp(simple.Offset, 0, GetDocumentLength());
        }

        private void RaiseDocumentTextChanged(string oldText, string newText)
        {
            var (prefix, oldLength, newLength) = TextSegmentation.ComputeChange(oldText, newText);

            if (oldLength == 0 && newLength == 0)
            {
                return;
            }

            _documentVersion++;

            var handler = _navTextChanged;
            if (handler is null)
            {
                return;
            }

            var position = CreateLocalPointer(prefix, LogicalDirection.Forward);
            handler(this, new TextChange(position, oldLength, newLength));
        }

        private static string GetTextLineText(TextLine textLine)
        {
            if (textLine.Length == 0)
            {
                return string.Empty;
            }

            var builder = StringBuilderCache.Acquire(textLine.Length);

            foreach (var run in textLine.TextRuns)
            {
                if (run.Length > 0)
                {
                    builder.Append(run.Text.Span);
                }
            }

            var lineText = builder.ToString();

            StringBuilderCache.Release(builder);

            return lineText;
        }

        public override void ExecuteContextMenuAction(ContextMenuAction action)
        {
            base.ExecuteContextMenuAction(action);

            switch (action)
            {
                case ContextMenuAction.Copy:
                    _parent?.Copy();
                    break;
                case ContextMenuAction.Cut:
                    _parent?.Cut();
                    break;
                case ContextMenuAction.Paste:
                    _parent?.Paste();
                    break;
                case ContextMenuAction.SelectAll:
                    _parent?.SelectAll();
                    break;
            }
        }

        private void OnParentPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == TextBox.TextProperty)
            {
                RaiseDocumentTextChanged(e.OldValue as string ?? string.Empty, e.NewValue as string ?? string.Empty);

                RaiseSurroundingTextChanged();

                if (_isInChange)
                    _textChanged = true;
                else
                    RaiseTextChangedCore();
            }

            if (e.Property == TextBox.SelectionStartProperty || e.Property == TextBox.SelectionEndProperty)
            {
                if (_isInChange)
                    _selectionChanged = true;
                else
                {
                    RaiseSelectionChanged();
                    RaiseCaretPositionChangedCore();
                }
            }
        }

        internal IDisposable BeginChange()
        {
            if (_isInChange)
                return Disposable.Empty;

            _isInChange = true;
            return Disposable.Create(RaiseEvents);
        }

        private void RaiseEvents()
        {
            _isInChange = false;

            if (_textChanged)
                RaiseTextChangedCore();

            if (_selectionChanged)
            {
                RaiseSelectionChanged();
                RaiseCaretPositionChangedCore();
            }

            _textChanged = false;
            _selectionChanged = false;
        }

        private string GetDocumentText() => _parent?.Text ?? string.Empty;

        private int GetDocumentLength() => GetDocumentText().Length;

        private ITextPointer CreateLocalPointer(int offset, LogicalDirection direction)
            => new SimpleTextPointer(this, Math.Clamp(offset, 0, GetDocumentLength()), direction);

        private ITextRange CreateLocalRange(int start, int end)
            => new SimpleTextRange(
                CreateLocalPointer(start, LogicalDirection.Forward),
                CreateLocalPointer(end, LogicalDirection.Backward));

        private int GetAbsoluteOffset(ITextPointer pointer)
        {
            if (pointer is null)
                throw new ArgumentNullException(nameof(pointer));

            if (pointer is SimpleTextPointer simplePointer)
            {
                return Math.Clamp(simplePointer.Offset, 0, GetDocumentLength());
            }

            return Math.Clamp(pointer.Offset, 0, GetDocumentLength());
        }

        private (int Start, int End) GetAbsoluteRange(ITextRange range)
        {
            if (range is null)
                throw new ArgumentNullException(nameof(range));

            var start = GetAbsoluteOffset(range.Start);
            var end = GetAbsoluteOffset(range.End);

            return start <= end ? (start, end) : (end, start);
        }

        private void ReplaceTextCore(int start, int end, string text, bool clearComposition)
        {
            if (_parent is null)
            {
                return;
            }

            var oldText = GetDocumentText();

            var safeStart = Math.Clamp(Math.Min(start, end), 0, oldText.Length);
            var safeEnd = Math.Clamp(Math.Max(start, end), 0, oldText.Length);

            var replacement = text ?? string.Empty;
            var newText = oldText.Remove(safeStart, safeEnd - safeStart).Insert(safeStart, replacement);

            _parent.Text = newText;

            var newCaret = safeStart + replacement.Length;
            _parent.SelectionStart = newCaret;
            _parent.SelectionEnd = newCaret;

            if (clearComposition)
            {
                SetCompositionRangeCore(null, raiseEvent: true);
            }
        }

        private void SetCompositionRangeCore(ITextRange? range, bool raiseEvent)
        {
            ITextRange? normalized = null;
            if (range is not null)
            {
                var (start, end) = GetAbsoluteRange(range);
                normalized = CreateLocalRange(start, end);
            }

            if (AreRangesEqual(_compositionRange, normalized))
            {
                return;
            }

            _compositionRange = normalized;
            if (raiseEvent)
            {
                RaiseCompositionChangedCore();
            }
        }

        private bool AreRangesEqual(ITextRange? left, ITextRange? right)
        {
            if (left is null || right is null)
            {
                return left is null && right is null;
            }

            return left.Start.Offset == right.Start.Offset &&
                   left.End.Offset == right.End.Offset;
        }

        private Rect TransformPresenterRect(Rect rect)
        {
            if (_parent is null || _presenter is null)
            {
                return rect;
            }

            var transform = _presenter.TransformToVisual(_parent);
            if (transform is null)
            {
                return rect;
            }

            return rect.TransformToAABB(transform.Value);
        }

        private Point TransformPointToPresenter(Point point)
        {
            if (_parent is null || _presenter is null)
            {
                return point;
            }

            var transform = _parent.TransformToVisual(_presenter);
            if (transform is null)
            {
                return point;
            }

            return point.Transform(transform.Value);
        }

        private ITextRange GetLineRange(int offset, string text)
        {
            var (start, end) = TextSegmentation.LineBounds(offset, text);
            return CreateLocalRange(start, end);
        }

        private ITextRange GetWordRange(int offset, string text)
        {
            var (start, end) = TextSegmentation.WordBounds(offset, text);
            return CreateLocalRange(start, end);
        }

        private ITextRange GetSentenceRange(int offset, string text)
        {
            var (start, end) = TextSegmentation.SentenceBounds(offset, text);
            return CreateLocalRange(start, end);
        }

        private void RaiseTextChangedCore() => TextChanged?.Invoke(this, EventArgs.Empty);

        private void RaiseCaretPositionChangedCore() => CaretPositionChanged?.Invoke(this, EventArgs.Empty);

        private void RaiseCompositionChangedCore() => CompositionChanged?.Invoke(this, EventArgs.Empty);

        private sealed class SimpleTextPointer : ITextPointer
        {
            public SimpleTextPointer(TextBoxTextInputMethodClient owner, int offset, LogicalDirection logicalDirection)
            {
                Owner = owner;
                Offset = offset;
                Gravity = logicalDirection;
            }

            public TextBoxTextInputMethodClient Owner { get; }

            public int Offset { get; }

            public LogicalDirection Gravity { get; }

            public int CompareTo(ITextPointer? other)
            {
                if (other is null)
                {
                    return 1;
                }

                return Offset.CompareTo(other.Offset);
            }

            // Equality is by document position; a flat-text store determines position entirely by
            // Offset, so gravity is intentionally ignored. Comparing pointers from different
            // navigators is undefined.
            public bool Equals(ITextPointer? other) => other is not null && Offset == other.Offset;

            public override bool Equals(object? obj) => obj is ITextPointer other && Equals(other);

            public override int GetHashCode() => Offset;
        }

        private sealed class SimpleTextRange : ITextRange
        {
            public SimpleTextRange(ITextPointer start, ITextPointer end)
            {
                Start = start;
                End = end;
            }

            public ITextPointer Start { get; }

            public ITextPointer End { get; }

            public bool IsEmpty => Start.Offset == End.Offset;
        }
    }
}
