using System;
using Avalonia.Reactive;

namespace Avalonia.Input.TextInput
{
    internal class TextInputMethodManager
    {
        private ITextInputMethodImpl? _im;
        private IInputElement? _focusedElement;
        private TextInputMethodClient? _client;
        private readonly TransformTrackingHelper _transformTracker = new TransformTrackingHelper();

        public TextInputMethodManager()
        {
            _transformTracker.MatrixChanged += UpdateCursorRect;
            InputMethod.IsInputMethodEnabledProperty.Changed.Subscribe(OnIsInputMethodEnabledChanged);
        }

        private TextInputMethodClient? Client
        {
            get => _client;
            set
            {
                if(_client == value)
                {
                    return;
                }

                if (_client != null)
                {
                    _client.CursorRectangleChanged -= OnCursorRectangleChanged;
                    _client.TextViewVisualChanged -= OnTextViewVisualChanged;

                    _client = null;

                    _im?.Reset();
                }

                _client = value;
                
                if (_client != null)
                {
                    _client.CursorRectangleChanged += OnCursorRectangleChanged;
                    _client.TextViewVisualChanged += OnTextViewVisualChanged;
                    
                    if (_focusedElement is StyledElement target)
                    {
                        _im?.SetOptions(TextInputOptions.FromStyledElement(target));
                    }
                    else
                    {
                        _im?.SetOptions(TextInputOptions.Default);
                    }

                    _transformTracker.SetVisual(_client?.TextViewVisual);
                    
                    _im?.SetClient(_client);

                    UpdateCursorRect();
                }
                else
                {
                    _im?.SetClient(null);
                    _transformTracker.SetVisual(null);
                }
            }
        }

        private void OnIsInputMethodEnabledChanged(AvaloniaPropertyChangedEventArgs<bool> obj)
        {
            if (ReferenceEquals(obj.Sender, _focusedElement))
            {
                TryFindAndApplyClient();
            }
        }

        private void OnTextViewVisualChanged(object? sender, EventArgs e) 
            => _transformTracker.SetVisual(_client?.TextViewVisual);

        private void UpdateCursorRect()
        {
            if (_im == null || 
                _client == null || 
                _focusedElement is not Visual v || 
                v.VisualRoot is not Visual root)
                return;

            var transform = v.TransformToVisual(root);
            if (transform == null)
                _im.SetCursorRect(default);
            else
                _im.SetCursorRect(_client.CursorRectangle.TransformToAABB(transform.Value));
        }

        private void OnCursorRectangleChanged(object? sender, EventArgs e)
        {
            if (sender == _client)
                UpdateCursorRect();
        }
        
        public void SetFocusedElement(IInputElement? element)
        {
            if(_focusedElement == element)
                return;
            _focusedElement = element;

            var inputMethod = ((element as Visual)?.VisualRoot as ITextInputMethodRoot)?.InputMethod;

            if (_im != inputMethod)
            {
                _im?.SetClient(null);
            }
            
            _im = inputMethod;

            TryFindAndApplyClient();
        }

        private void TryFindAndApplyClient()
        {
            if (_focusedElement is not InputElement focused ||
                _im == null ||
                !InputMethod.GetIsInputMethodEnabled(focused))
            {
                Client = null;
                return;
            }

            var clientQuery = new TextInputMethodClientRequestedEventArgs
            {
                RoutedEvent = InputElement.TextInputMethodClientRequestedEvent
            };

            _focusedElement.RaiseEvent(clientQuery);
            Client = clientQuery.Client;
        }
    }
}
