using System;
using System.Collections.Generic;
using Avalonia.Controls.Presenters;
using Avalonia.Input.TextInput;
using Avalonia.Media.TextFormatting;
using Avalonia.Reactive;
using Avalonia.Utilities;

namespace Avalonia.Controls
{
    internal class TextBoxTextInputMethodClient : TextInputMethodClient, IStructuredTextInput
    {
        private TextBox? _parent;
        private TextPresenter? _presenter;
        private bool _selectionChanged;
        private bool _textChanged;
        private bool _isInChange;
        private EventHandler? _caretBoundsChangedHandler;
        private ITextRange? _compositionRange;

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
                oldPresenter.ClearValue(TextPresenter.PreeditTextProperty);

                if (_caretBoundsChangedHandler != null)
                {
                    oldPresenter.CaretBoundsChanged -= _caretBoundsChangedHandler;
                }
            }

            _presenter = presenter;

            if (_presenter != null)
            {
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

        ITextPointer IStructuredTextInput.DocumentStart => CreateLocalPointer(0, LogicalDirection.Forward);

        ITextPointer IStructuredTextInput.DocumentEnd =>
            CreateLocalPointer(GetDocumentLength(), LogicalDirection.Backward);

        ITextRange IStructuredTextInput.DocumentRange =>
            CreateLocalRange(0, GetDocumentLength());

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

        string IStructuredTextInput.GetText(ITextRange range)
        {
            var text = GetDocumentText();
            var (start, end) = GetAbsoluteRange(range);

            return text.Substring(start, end - start);
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

            return CreateLocalPointer(clamped, closest.LogicalDirection);
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

        ITextRange? IStructuredTextInput.GetRangeEnclosing(ITextPointer position, TextGranularity granularity)
        {
            var offset = GetAbsoluteOffset(position);
            var text = GetDocumentText();
            var length = text.Length;

            switch (granularity)
            {
                case TextGranularity.Document:
                    return CreateLocalRange(0, length);

                case TextGranularity.Character:
                    if (length == 0)
                    {
                        return CreateLocalRange(0, 0);
                    }

                    var characterStart = Math.Clamp(offset, 0, Math.Max(0, length - 1));
                    return CreateLocalRange(characterStart, Math.Min(length, characterStart + 1));

                case TextGranularity.Line:
                case TextGranularity.Paragraph:
                    return GetLineRange(offset, text);

                case TextGranularity.Word:
                    return GetWordRange(offset, text);

                case TextGranularity.Sentence:
                    return GetSentenceRange(offset, text);

                default:
                    return null;
            }
        }

        ITextPointer? IStructuredTextInput.GetBoundaryPosition(ITextPointer position, TextGranularity granularity,
            LogicalDirection direction)
        {
            var offset = GetAbsoluteOffset(position);
            var length = GetDocumentLength();

            if (granularity == TextGranularity.Document)
            {
                return direction == LogicalDirection.Forward
                    ? CreateLocalPointer(length, LogicalDirection.Forward)
                    : CreateLocalPointer(0, LogicalDirection.Backward);
            }

            if (granularity == TextGranularity.Character)
            {
                var characterBoundary = direction == LogicalDirection.Forward
                    ? Math.Min(length, offset + 1)
                    : Math.Max(0, offset - 1);

                return CreateLocalPointer(characterBoundary, direction);
            }

            var enclosing = ((IStructuredTextInput)this).GetRangeEnclosing(position, granularity);
            if (enclosing is null)
            {
                return null;
            }

            return direction == LogicalDirection.Forward
                ? CreateLocalPointer(enclosing.End.Offset, LogicalDirection.Forward)
                : CreateLocalPointer(enclosing.Start.Offset, LogicalDirection.Backward);
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
#if NET6_0_OR_GREATER
                    builder.Append(run.Text.Span);
#else
                    builder.Append(run.Text.Span.ToArray());
#endif
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
            var length = text.Length;
            if (length == 0)
            {
                return CreateLocalRange(0, 0);
            }

            var position = Math.Clamp(offset, 0, length - 1);
            var start = position;
            while (start > 0 && text[start - 1] != '\n' && text[start - 1] != '\r')
            {
                start--;
            }

            var end = position;
            while (end < length && text[end] != '\n' && text[end] != '\r')
            {
                end++;
            }

            return CreateLocalRange(start, end);
        }

        private ITextRange GetWordRange(int offset, string text)
        {
            var length = text.Length;
            if (length == 0)
            {
                return CreateLocalRange(0, 0);
            }

            var position = Math.Clamp(offset, 0, length - 1);
            if (!IsWordCharacter(text[position]))
            {
                return CreateLocalRange(position, position);
            }

            var start = position;
            while (start > 0 && IsWordCharacter(text[start - 1]))
            {
                start--;
            }

            var end = position + 1;
            while (end < length && IsWordCharacter(text[end]))
            {
                end++;
            }

            return CreateLocalRange(start, end);
        }

        private ITextRange GetSentenceRange(int offset, string text)
        {
            var length = text.Length;
            if (length == 0)
            {
                return CreateLocalRange(0, 0);
            }

            var position = Math.Clamp(offset, 0, length - 1);

            var start = position;
            while (start > 0 && !IsSentenceBoundary(text[start - 1]))
            {
                start--;
            }

            var end = position;
            while (end < length && !IsSentenceBoundary(text[end]))
            {
                end++;
            }

            if (end < length)
            {
                end++;
            }

            return CreateLocalRange(start, end);
        }

        private static bool IsWordCharacter(char c) =>
            char.IsLetterOrDigit(c) || c == '_';

        private static bool IsSentenceBoundary(char c) =>
            c is '.' or '!' or '?' or '\n' or '\r';

        private void RaiseTextChangedCore() => TextChanged?.Invoke(this, EventArgs.Empty);

        private void RaiseCaretPositionChangedCore() => CaretPositionChanged?.Invoke(this, EventArgs.Empty);

        private void RaiseCompositionChangedCore() => CompositionChanged?.Invoke(this, EventArgs.Empty);

        private sealed class SimpleTextPointer : ITextPointer
        {
            public SimpleTextPointer(TextBoxTextInputMethodClient owner, int offset, LogicalDirection logicalDirection)
            {
                Owner = owner;
                Offset = offset;
                LogicalDirection = logicalDirection;
            }

            public TextBoxTextInputMethodClient Owner { get; }

            public int Offset { get; }

            public LogicalDirection LogicalDirection { get; }

            public int CompareTo(ITextPointer? other)
            {
                if (other is null)
                {
                    return 1;
                }

                return Offset.CompareTo(other.Offset);
            }
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
