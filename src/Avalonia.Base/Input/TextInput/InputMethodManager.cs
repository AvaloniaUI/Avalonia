using System;
using Avalonia.Interactivity;
using Avalonia.Reactive;

namespace Avalonia.Input.TextInput
{
    internal class TextInputMethodManager
    {
        private ITextInputMethodImpl? _im;
        private IInputElement? _focusedElement;
        private Interactive? _visualRoot;
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
                    _client.ResetRequested -= OnResetRequested;

                    _client = null;

                    _im?.Reset();
                }

                _client = value;
                
                if (_client != null)
                {
                    _client.CursorRectangleChanged += OnCursorRectangleChanged;
                    _client.TextViewVisualChanged += OnTextViewVisualChanged;
                    _client.ResetRequested += OnResetRequested;
                    
                    PopulateImWithInitialValues();
                }
                else
                {
                    _im?.SetClient(null);
                    _transformTracker.SetVisual(null);
                }
            }
        }

        void PopulateImWithInitialValues()
        {
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

        private void OnResetRequested(object? sender, EventArgs args)
        {
            if (_im != null && sender == _client)
            {
                _im.Reset();
                PopulateImWithInitialValues();
            }
        }

        private void OnIsInputMethodEnabledChanged(AvaloniaPropertyChangedEventArgs<bool> obj)
        {
            if (ReferenceEquals(obj.Sender, _focusedElement))
            {
                RediscoverInputMethodAndClient();
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

            if (_visualRoot != null)
                InputMethod.RemoveTextInputMethodClientRequeryRequestedHandler(_visualRoot,
                    TextInputMethodClientRequeryRequested);
            
            _focusedElement = element;

            _visualRoot = (element as Visual)?.VisualRoot as Interactive;
            if (_visualRoot != null)
                InputMethod.AddTextInputMethodClientRequeryRequestedHandler(_visualRoot,
                    TextInputMethodClientRequeryRequested);
            
            RediscoverInputMethodAndClient();
        }
        
        void RediscoverInputMethodAndClient()
        {
            var (inputMethod, inputClient) = FindInputMethodAndClient();
            
            // Reset the previous input method and our state on input method change
            if (_im != inputMethod) 
                Client = null;
            
            _im = inputMethod;
            
            Client = inputClient;
        }

        private void TextInputMethodClientRequeryRequested(object? sender, RoutedEventArgs e)
        {
            if (_im != null)
                RediscoverInputMethodAndClient();
        }
        
        private (ITextInputMethodImpl? im, TextInputMethodClient? client) FindInputMethodAndClient()
        {
            if (_focusedElement is not InputElement focused ||
                _im == null ||
                !InputMethod.GetIsInputMethodEnabled(focused))
            {
                // Input method system is disabled by focused element
                return (null, null);
            }

            // Attempt to get a user-provided input method
            var imQuery = new PluggableTextInputMethodRequestedEventArgs()
            {
                RoutedEvent = PluggableTextInputMethod.TextInputMethodRequestedEvent
            };
            _focusedElement.RaiseEvent(imQuery);
            
            // Fall back to the system provided one, if any
            var im = imQuery.InputMethod?.Adapter 
                     ?? (focused.VisualRoot as ITextInputMethodRoot)?.InputMethod;

            if (im == null)
                return (null, null);

            var clientQuery = new TextInputMethodClientRequestedEventArgs
            {
                RoutedEvent = InputElement.TextInputMethodClientRequestedEvent
            };

            _focusedElement.RaiseEvent(clientQuery);
            return (im, clientQuery.Client);
        }
    }
}
