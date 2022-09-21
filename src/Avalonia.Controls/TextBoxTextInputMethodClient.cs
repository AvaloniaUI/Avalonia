using System;
using Avalonia.Controls.Presenters;
using Avalonia.Input.TextInput;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    internal class TextBoxTextInputMethodClient : ITextInputMethodClient
    {
        private TextBox? _parent;
        private TextPresenter? _presenter;

        public IVisual TextViewVisual => _presenter!;

        public bool SupportsPreedit => true;

        public bool SupportsSurroundingText => true;

        public Rect CursorRectangle
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

                var rect = _presenter.GetCursorRectangle().TransformToAABB(transform.Value);

                return rect;
            }
        }

        public TextInputMethodSurroundingText SurroundingText
        {
            get
            {
                if(_presenter is null || _parent is null)
                {
                    return default;
                }

                var lineIndex = _presenter.TextLayout.GetLineIndexFromCharacterIndex(_presenter.CaretIndex, false);

                var textLine = _presenter.TextLayout.TextLines[lineIndex];

                var lineStart = textLine.FirstTextSourceIndex;

                var lineText = _presenter.Text?.Substring(lineStart, textLine.Length);

                var anchorOffset = Math.Max(0, _parent.SelectionStart - lineStart);

                var cursorOffset = Math.Max(0, _presenter.SelectionEnd - lineStart);

                return new TextInputMethodSurroundingText
                {
                    Text = lineText ?? "",                   
                    AnchorOffset = anchorOffset,
                    CursorOffset = cursorOffset
                };
            }
        }

        public event EventHandler? TextViewVisualChanged;

        public event EventHandler? CursorRectangleChanged;

        public event EventHandler? SurroundingTextChanged;

        public void SetPreeditText(string? text)
        {
            if (_presenter == null)
            {
                return;
            }

            _presenter.PreeditText = text;
        }

        public void SelectInSurroundingText(int start, int end)
        {
            if(_parent is null ||_presenter is null)
            {
                return;
            }

            var lineIndex = _presenter.TextLayout.GetLineIndexFromCharacterIndex(_presenter.CaretIndex, false);

            var textLine = _presenter.TextLayout.TextLines[lineIndex];

            var lineStart = textLine.FirstTextSourceIndex;

            var selectionStart = lineStart + start;
            var selectionEnd = lineStart + end;
             
            _parent.SelectionStart = selectionStart;
            _parent.SelectionEnd = selectionEnd;
        }    
        
        public void SetPresenter(TextPresenter? presenter, TextBox? parent)
        {
            if(_parent != null)
            {
                _parent.PropertyChanged -= OnParentPropertyChanged;
            }

            _parent = parent;

            if(_parent != null)
            {
                _parent.PropertyChanged += OnParentPropertyChanged;
            }

            if (_presenter != null)
            {
                _presenter.PreeditText = null;

                _presenter.CaretBoundsChanged -= OnCaretBoundsChanged;             
            }
           
            _presenter = presenter;
            
            if (_presenter != null)
            {
                _presenter.CaretBoundsChanged += OnCaretBoundsChanged;
            }
           
            TextViewVisualChanged?.Invoke(this, EventArgs.Empty);

            OnCaretBoundsChanged(this, EventArgs.Empty);
        }

        private void OnParentPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if(e.Property == TextBox.SelectionStartProperty || e.Property == TextBox.SelectionEndProperty)
            {
                if (SupportsSurroundingText)
                {
                    SurroundingTextChanged?.Invoke(this, e);
                }
            }
        }

        public void DeleteSurroundingText(int beforeLength, int afterLength)
        {
            if (_parent != null && _presenter != null && string.IsNullOrEmpty(_presenter.PreeditText))
            {
                var start = _presenter.SelectionStart;

                _parent.SelectionStart = start - beforeLength;
                _parent.SelectionEnd = start + afterLength;

                _parent.DeleteSelection(true);
            }
        }

        private void OnCaretBoundsChanged(object? sender, EventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                CursorRectangleChanged?.Invoke(this, e);

            }, DispatcherPriority.Input);
        }
    }
}
