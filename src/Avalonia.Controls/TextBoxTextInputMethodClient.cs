using System;
using Avalonia.Controls.Presenters;
using Avalonia.Input.TextInput;
using Avalonia.Reactive;

namespace Avalonia.Controls
{
    internal class TextBoxTextInputMethodClient : TextInputMethodClient
    {
        private TextBox? _parent;
        private TextPresenter? _presenter;
        private bool _selectionChanged;
        private bool _isInChange;
        private EventHandler? _caretBoundsChangedHandler;

        public override Visual TextViewVisual => _presenter!;

        public override string SurroundingText
        {
            get
            {
                if (_parent is null)
                {
                    return "";
                }

                if (_presenter is not null && _parent.CaretIndex != _presenter.CaretIndex)
                {
                    _presenter.SetCurrentValue(TextPresenter.CaretIndexProperty, _parent.CaretIndex);
                }

                if (_presenter is not null && _parent.Text != _presenter.Text)
                {
                    _presenter.SetCurrentValue(TextPresenter.TextProperty, _parent.Text);
                }

                return _parent.Text ?? string.Empty;
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
                if (_parent is null)
                {
                    return default;
                }

                return new TextSelection(_parent.SelectionStart, _parent.SelectionEnd);
            }
            set
            {
                if (_parent is null)
                {
                    return;
                }

                _parent.SelectionStart = value.Start;
                _parent.SelectionEnd = value.End;

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

                if (_caretBoundsChangedHandler is not null)
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
