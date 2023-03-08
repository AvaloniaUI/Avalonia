using System;
using Avalonia.Input.TextInput;
using Avalonia.Native.Interop;

namespace Avalonia.Native
{
    internal class AvaloniaNativeTextInputMethod : ITextInputMethodImpl, IDisposable
    {
        private ITextInputMethodClient _client;
        private IAvnTextInputMethodClient _nativeClient;
        private readonly IAvnTextInputMethod _inputMethod;
        
        public AvaloniaNativeTextInputMethod(IAvnWindowBase nativeWindow)
        {
            _inputMethod = nativeWindow.InputMethod;
        }

        public void Dispose()
        {
            _inputMethod.Dispose();
            _nativeClient?.Dispose();
        }

        public void Reset()
        {
            _inputMethod.Reset();
        }

        public void SetClient(ITextInputMethodClient client)
        {
            if (_client is { SupportsSurroundingText: true })
            {
                _client.SurroundingTextChanged -= OnSurroundingTextChanged;
                _client.CursorRectangleChanged -= OnCursorRectangleChanged;
                
                _nativeClient?.Dispose();
            }
            
            _nativeClient = null;
            _client = client;
            
            if (client != null)
            {
                _nativeClient = new AvnTextInputMethodClient(client);

                OnSurroundingTextChanged(this, EventArgs.Empty);
                OnCursorRectangleChanged(this, EventArgs.Empty);

                _client.SurroundingTextChanged += OnSurroundingTextChanged;
                _client.CursorRectangleChanged += OnCursorRectangleChanged;
            }

            _inputMethod.SetClient(_nativeClient);
        }

        private void OnCursorRectangleChanged(object sender, EventArgs e)
        {
            if (_client == null)
            {
                return;
            }

            _inputMethod.SetCursorRect(_client.CursorRectangle.ToAvnRect());
        }

        private void OnSurroundingTextChanged(object sender, EventArgs e)
        {
            if (_client == null)
            {
                return;
            }
            
            var surroundingText = _client.SurroundingText;

            _inputMethod.SetSurroundingText(
                surroundingText.Text,
                surroundingText.AnchorOffset,
                surroundingText.CursorOffset
            );
        }

        public void SetCursorRect(Rect rect)
        {
            _inputMethod.SetCursorRect(rect.ToAvnRect());
        }

        public void SetOptions(TextInputOptions options)
        {
           
        }

        private class AvnTextInputMethodClient : NativeCallbackBase, IAvnTextInputMethodClient
        {
            private readonly ITextInputMethodClient _client;

            public AvnTextInputMethodClient(ITextInputMethodClient client)
            {
                _client = client;
            }

            public void SetPreeditText(string preeditText)
            {
                if (_client.SupportsPreedit)
                {
                    _client.SetPreeditText(preeditText);
                }
            }

            public void SelectInSurroundingText(int start, int end)
            {
                if (_client.SupportsSurroundingText)
                {
                    _client.SelectInSurroundingText(start, end);
                }
            }
        }
    }
}
