using System;
using System.Text;
using Avalonia.Controls.Presenters;
using Avalonia.Input.TextInput;
using Avalonia.Media.TextFormatting;
using Avalonia.Reactive;
using Avalonia.Utilities;

namespace Avalonia.Controls
{
    internal class TextBoxTextInputMethodClient : TextInputMethodClient
    {
        private TextBox? _parent;
        private TextPresenter? _presenter;
        private bool _selectionChanged;
        private bool _isInChange;

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

        internal override string Text => _presenter?.Text ?? string.Empty;

        internal override string GetTextBeforeCaret(int length)
        {
            if (_presenter is null || _parent is null)
            {
                return "";
            }
            var selectionStart = _presenter.SelectionStart;

            var lineIndex = _presenter.TextLayout.GetLineIndexFromCharacterIndex(selectionStart, false);

            var textLine = _presenter.TextLayout.TextLines[lineIndex];

            var offset = selectionStart - textLine.FirstTextSourceIndex;

            var currentLineLength = Math.Min(offset, length);
            var start = Math.Max(offset - currentLineLength, 0);

            var lineText = GetTextLineText(textLine);
            var text = lineText.Substring(start, currentLineLength);

            var newText = text;

            length -= currentLineLength;

            while (length > 0)
            {
                lineIndex--;
                if (lineIndex >= 0)
                {
                    textLine = _presenter.TextLayout.TextLines[lineIndex];
                    currentLineLength = Math.Min(textLine.Length, length);

                    lineText = GetTextLineText(textLine);
                    text = lineText.Substring(textLine.Length - currentLineLength, currentLineLength);

                    newText = text + newText;

                    length -= currentLineLength;
                }
                else
                    break;
            }

            return newText;
        }

        internal override string GetTextAfterCaret(int length)
        {
            if (_presenter is null || _parent is null)
            {
                return "";
            }

            var selectionEnd = _presenter.SelectionStart;

            var lineIndex = _presenter.TextLayout.GetLineIndexFromCharacterIndex(selectionEnd, false);

            var textLine = _presenter.TextLayout.TextLines[lineIndex];
            var lastIndex = textLine.FirstTextSourceIndex + textLine.Length;

            var currentLineLength = Math.Min(lastIndex - selectionEnd, length);
            var start = Math.Max(selectionEnd - textLine.FirstTextSourceIndex, 0);

            var builder = new StringBuilder();

            var lineText = GetTextLineText(textLine);

            builder.Append(lineText.Substring(start, currentLineLength));

            length -= currentLineLength;

            while (length > 0)
            {
                lineIndex++;
                if (lineIndex < _presenter.TextLayout.TextLines.Count)
                {
                    textLine = _presenter.TextLayout.TextLines[lineIndex];
                    currentLineLength = Math.Min(textLine.Length, length);

                    lineText = GetTextLineText(textLine);
                    var text = lineText.Substring(0, currentLineLength);

                    builder.Append(text);

                    length -= currentLineLength;
                }
                else
                    break;
            }

            return builder.ToString();
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

        internal override TextSelection ActualSelection
        {
            get
            {
                if (_presenter is null || _parent is null)
                {
                    return default;
                }

                return new TextSelection(_presenter.SelectionStart, _presenter.SelectionEnd);
            }

            set
            {
                if (_parent is not null)
                {
                    _parent.SelectionStart = value.Start;
                    _parent.SelectionEnd = value.End;

                    RaiseSelectionChanged();
                }
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

                oldPresenter.CaretBoundsChanged -= (s, e) => RaiseCursorRectangleChanged();
            }

            _presenter = presenter;

            if (_presenter != null)
            {
                _presenter.CaretBoundsChanged += (s, e) => RaiseCursorRectangleChanged();
            }

            RaiseTextViewVisualChanged();

            RaiseCursorRectangleChanged();
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
            }

            if (e.Property == TextBox.SelectionStartProperty || e.Property == TextBox.SelectionEndProperty)
            {
                if (_isInChange)
                    _selectionChanged = true;
                else
                    RaiseSelectionChanged();
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

            if (_selectionChanged)
                RaiseSelectionChanged();

            _selectionChanged = false;
        }
    }
}
