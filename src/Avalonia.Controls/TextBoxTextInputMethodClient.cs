using System;
using Avalonia.Controls.Presenters;
using Avalonia.Input.TextInput;
using Avalonia.Media.TextFormatting;
using Avalonia.Utilities;

namespace Avalonia.Controls
{
    internal class TextBoxTextInputMethodClient : TextInputMethodClient
    {
        private TextBox? _parent;
        private TextPresenter? _presenter;
        private bool _isPropertyChange;

        public override Visual TextViewVisual => _presenter!;

        public override bool SupportsPreedit => true;

        public override bool SupportsSurroundingText => true;

        public void SetPresenter(TextPresenter? presenter, TextBox? parent)
        {
            if (_parent != null)
            {
                _parent.PropertyChanged -= OnParentPropertyChanged;
            }

            _parent = parent;

            if (_parent != null)
            {
                _parent.PropertyChanged += OnParentPropertyChanged;
            }

            var oldPresenter = _presenter;

            if (oldPresenter != null)
            {
                oldPresenter.ClearValue(TextPresenter.PreeditTextProperty);

                oldPresenter.CaretBoundsChanged -= OnPresenterCursorRectangleChanged;
            }

            _presenter = presenter;

            if (_presenter != null)
            {
                _presenter.CaretBoundsChanged += OnPresenterCursorRectangleChanged;
            }

            OnTextViewVisualChanged(oldPresenter, presenter);

            OnPresenterCursorRectangleChanged(this, EventArgs.Empty);
        }

        public override void SetPreeditText(string? preeditText)
        {
            if (_presenter == null || _parent == null)
            {
                return;
            }

            _presenter.SetCurrentValue(TextPresenter.PreeditTextProperty, preeditText);
        }

        protected override void OnSelectionChanged(TextSelection oldValue, TextSelection newValue)
        {
            base.OnSelectionChanged(oldValue, newValue);

            if (_isPropertyChange)
            {
                return;
            }

            if (oldValue != newValue)
            {
                SetParentSelection(newValue);
            }
        }

        private static string GetTextLineText(TextLine textLine)
        {
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

        private void OnParentTextChanged()
        {
            if (_presenter is null || _parent is null)
            {
                SurroundingText = "";

                return;
            }

#if DEBUG
            if (_parent.CaretIndex != _presenter.CaretIndex)
            {
                throw new InvalidOperationException("TextBox and TextPresenter are out of sync");
            }

            if (_parent.Text != _presenter.Text)
            {
                throw new InvalidOperationException("TextBox and TextPresenter are out of sync");
            }
#endif

            var lineIndex = _presenter.TextLayout.GetLineIndexFromCharacterIndex(_presenter.CaretIndex, false);

            var textLine = _presenter.TextLayout.TextLines[lineIndex];

            var lineText = GetTextLineText(textLine);

            SurroundingText = lineText;
        }

        private void OnPresenterCursorRectangleChanged(object? sender, EventArgs e)
        {
            if (_parent == null || _presenter == null)
            {
                CursorRectangle = default;

                return;
            }

            var transform = _presenter.TransformToVisual(_parent);

            if (transform == null)
            {
                CursorRectangle = default;

                return;
            }

            CursorRectangle = _presenter.GetCursorRectangle().TransformToAABB(transform.Value);
        }

        private void OnParentPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            _isPropertyChange = true;

            if (e.Property == TextBox.TextProperty)
            {
                OnParentTextChanged();
            }

            if (e.Property == TextBox.SelectionStartProperty || e.Property == TextBox.SelectionEndProperty)
            {
                Selection = GetParentSelection();
            }

            _isPropertyChange = false;
        }

        private TextSelection GetParentSelection()
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

        private void SetParentSelection(TextSelection selection)
        {
            if (_parent is null || _presenter is null)
            {
                return;
            }

            var lineIndex = _presenter.TextLayout.GetLineIndexFromCharacterIndex(_parent.CaretIndex, false);

            var textLine = _presenter.TextLayout.TextLines[lineIndex];

            var lineStart = textLine.FirstTextSourceIndex;

            var selectionStart = lineStart + selection.Start;
            var selectionEnd = lineStart + selection.End;

            _parent.SelectionStart = selectionStart;
            _parent.SelectionEnd = selectionEnd;
        }
    }
}
