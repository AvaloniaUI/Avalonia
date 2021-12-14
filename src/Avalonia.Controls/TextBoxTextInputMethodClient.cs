using System;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    internal class TextBoxTextInputMethodClient : ITextInputMethodClient
    {
        private InputElement _parent;
        private TextPresenter _presenter;
        private IDisposable _subscription;
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
                return _presenter.GetCursorRectangle().TransformToAABB(transform.Value);
            }
        }

        public event EventHandler CursorRectangleChanged;
        public IVisual TextViewVisual => _presenter;
        public event EventHandler TextViewVisualChanged;
        public bool SupportsPreedit => false;
        public void SetPreeditText(string text) => throw new NotSupportedException();

        public bool SupportsSurroundingText => false;
        public TextInputMethodSurroundingText SurroundingText => throw new NotSupportedException();
        public event EventHandler SurroundingTextChanged { add { } remove { } }
        public string TextBeforeCursor => null;
        public string TextAfterCursor => null;

        private void OnCaretIndexChanged(int index) => CursorRectangleChanged?.Invoke(this, EventArgs.Empty);


        public void SetPresenter(TextPresenter presenter, InputElement parent)
        {
            _parent = parent;
            _subscription?.Dispose();
            _subscription = null;
            _presenter = presenter;
            if (_presenter != null)
            {
                _subscription = _presenter.GetObservable(TextPresenter.CaretIndexProperty)
                    .Subscribe(OnCaretIndexChanged);
            }
            TextViewVisualChanged?.Invoke(this, EventArgs.Empty);
            CursorRectangleChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
