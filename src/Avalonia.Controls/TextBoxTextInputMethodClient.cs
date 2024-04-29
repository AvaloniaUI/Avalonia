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

                oldPresenter.CaretBoundsChanged -= (s,e) => RaiseCursorRectangleChanged();
            }

            _presenter = presenter;

            if (_presenter != null)
            {
                _presenter.CaretBoundsChanged += (s, e) => RaiseCursorRectangleChanged();
            }

            RaiseTextViewVisualChanged();

            RaiseCursorRectangleChanged();
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

        private void OnParentPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == TextBox.TextProperty)
            {
                RaiseSurroundingTextChanged();
            }

            if (e.Property == TextBox.SelectionStartProperty || e.Property == TextBox.SelectionEndProperty)
            {
                RaiseSelectionChanged();
            }
        }
    }
}
