using System.Runtime.InteropServices.JavaScript;
using Avalonia.Browser.Interop;
using Avalonia.Input.TextInput;

namespace Avalonia.Browser.Text
{
    internal class ClassicInputMethod(JSObject inputElement) : IInputMethod
    {
        private TextInputMethodClient? _client;

        public void ClearInput()
        {
            InputHelper.ClearInputElement(inputElement);
        }

        public void Reset()
        {
            InputHelper.ClearInputElement(inputElement);
            InputHelper.SetSurroundingText(inputElement, "", 0, 0);
        }

        public void SetClient(TextInputMethodClient? client)
        {
            _client = client;
        }

        public void SetCursorRect(Rect rect)
        {
            InputHelper.FocusElement(inputElement);
            InputHelper.SetBounds(inputElement, (int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height,
                _client?.Selection.End ?? 0);
            InputHelper.FocusElement(inputElement);
        }

        public void SetOptions(TextInputOptions options)
        {
        }

        public void SetSurroundingText(string surroundingText, int start, int end)
        {
            InputHelper.SetSurroundingText(inputElement, surroundingText, start, end);
        }
    }
}
