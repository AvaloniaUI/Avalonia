using System.Runtime.InteropServices.JavaScript;
using Avalonia.Browser.Interop;
using Avalonia.Input.TextInput;

namespace Avalonia.Browser.Text
{
    internal class EditContextInputMethod : IInputMethod
    {
        private readonly JSObject _inputElement;
        private readonly int _topLevelId;
        private JSObject? _editContextHandle;
        private JSObject? _subscription;
        private TextInputMethodClient? _client;

        public bool IsUpdating { get; internal set; }
        internal EditContext? EditContext { get; set; }

        public EditContextInputMethod(JSObject inputElement, int topLevelId)
        {
            _inputElement = inputElement;
            _topLevelId = topLevelId;
        }

        public void ClearInput()
        {
            if (_editContextHandle != null)
                InputHelper.ClearText(_editContextHandle);
        }

        public void Reset()
        {
            ClearInput();
        }

        public void SetClient(TextInputMethodClient? client)
        {
            if (client != null)
            {
                _editContextHandle = InputHelper.AttachEditContext(_inputElement);
                EditContext = new EditContext(_editContextHandle);
                _subscription = InputHelper.SubscribeEditContextEvents(_editContextHandle, _topLevelId);
            }
            else
            {
                if (_subscription != null)
                    GeneralHelpers.IntCallLambda(_subscription);
                _subscription = null;
                InputHelper.DetachEditContext(_inputElement);
                _editContextHandle = null;
                EditContext = null;
            }

            _client = client;
        }

        public void SetCursorRect(Rect rect)
        {
            UpdateCharacterBounds();
        }

        public void SetOptions(TextInputOptions options)
        {

        }

        public void SetSurroundingText(string surroundingText, int selectionStart, int selectionEnd)
        {
            if (IsUpdating)
                return;

            if (EditContext is { } editContext && _editContextHandle != null)
            {
                InputHelper.ClearText(_editContextHandle);
                InputHelper.UpdateText(_editContextHandle, 0, 0, surroundingText);
                InputHelper.UpdateSelection(_editContextHandle, selectionStart, selectionEnd);
            }
        }

        internal void UpdateCharacterBounds()
        {
            if(_client != null && _editContextHandle != null)
            {
                var rect = _client.CursorRectangle;
                InputHelper.UpdateCharacterBounds(_editContextHandle, _client.Selection.Start, (int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
            }
        }

        internal void SetSelection(TextSelection selection)
        {
            if (IsUpdating)
                return;
            if (_client != null && _editContextHandle != null)
            {
                InputHelper.UpdateSelection(_editContextHandle, selection.Start, selection.End);
            }
        }
    }
}
