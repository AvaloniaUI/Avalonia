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
        private InputElement? _parent;
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
                
                var rect =  _presenter.GetCursorRectangle().TransformToAABB(transform.Value);

                return rect;
            }
        }

        public event EventHandler? CursorRectangleChanged;
        public IVisual TextViewVisual => _presenter!;
        public event EventHandler? TextViewVisualChanged;
        public bool SupportsPreedit => false;
        public void SetPreeditText(string text) => throw new NotSupportedException();

        public bool SupportsSurroundingText => false;
        public TextInputMethodSurroundingText SurroundingText => throw new NotSupportedException();
        public event EventHandler? SurroundingTextChanged { add { } remove { } }
        public string? TextBeforeCursor => null;
        public string? TextAfterCursor => null;

        private void OnCaretBoundsChanged(object? sender, EventArgs e) => CursorRectangleChanged?.Invoke(this, EventArgs.Empty);


        public void SetPresenter(TextPresenter? presenter, InputElement? parent)
        {
            _parent = parent;

            if (_presenter != null)
            {
                _presenter.CaretBoundsChanged -= OnCaretBoundsChanged;
            }
           
            _presenter = presenter;
            
            if (_presenter != null)
            {
                _presenter.CaretBoundsChanged += OnCaretBoundsChanged;
            }
            
            TextViewVisualChanged?.Invoke(this, EventArgs.Empty);
            CursorRectangleChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
