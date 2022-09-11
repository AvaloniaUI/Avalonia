using System;
using System.Diagnostics;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    internal class TextBoxTextInputMethodClient : ITextInputMethodClient
    {
        private TextBox? _parent;
        private TextPresenter? _presenter;

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

        public event EventHandler? CursorRectangleChanged;
        public IVisual TextViewVisual => _presenter!;
        public event EventHandler? TextViewVisualChanged;
        public bool SupportsPreedit => true;

        public void SetPreeditText(string? text)
        {
            if (_presenter == null)
            {
                return;
            }

            _presenter.PreeditText = text;
        }

        public bool SupportsSurroundingText => true;

        public event EventHandler? SurroundingTextChanged;

        public TextInputMethodSurroundingText SurroundingText => new()
        {
            Text = _presenter?.Text ?? "",
            CursorOffset = _presenter?.CaretIndex ?? 0,
            AnchorOffset = _presenter?.SelectionStart ?? 0
        };

        public string? TextBeforeCursor => null;
        
        public string? TextAfterCursor => null;
        public void SelectInSurroundingText(int start, int end)
        {
            if(_parent == null)
                return;
            // TODO: Account for the offset
            _parent.SelectionStart = start;
            _parent.SelectionEnd = end;
        }

        private void OnCaretBoundsChanged(object? sender, EventArgs e) => CursorRectangleChanged?.Invoke(this, EventArgs.Empty);
        
        private void OnTextBoxPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == TextBox.TextProperty || e.Property == TextBox.SelectionStartProperty ||
                e.Property == TextBox.SelectionEndProperty)
                SurroundingTextChanged?.Invoke(this, EventArgs.Empty);
        }


        public void SetPresenter(TextPresenter? presenter, TextBox? parent)
        {
            if (_parent != null)
            {
                _parent.PropertyChanged -= OnTextBoxPropertyChanged;
            }
            
            _parent = parent;

            if (_parent != null)
            {
                _parent.PropertyChanged += OnTextBoxPropertyChanged;
            }

            if (_presenter != null)
            {
                _presenter.CaretBoundsChanged -= OnCaretBoundsChanged;
            }
           
            _presenter = presenter;
            
            if (_presenter != null)
            {
                _presenter.CaretBoundsChanged += OnCaretBoundsChanged;
            }

            if(presenter == null)
            {
                SetPreeditText(null);
            }
            
            TextViewVisualChanged?.Invoke(this, EventArgs.Empty);
            CursorRectangleChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
