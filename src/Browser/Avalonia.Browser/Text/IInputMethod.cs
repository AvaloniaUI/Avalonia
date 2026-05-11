using Avalonia.Input.TextInput;

namespace Avalonia.Browser.Text
{
    internal interface IInputMethod
    {
        void ClearInput();
        void Reset();
        void SetClient(TextInputMethodClient? client);
        void SetCursorRect(Rect rect);
        void SetOptions(TextInputOptions options);
        void SetSurroundingText(string surroundingText, int start, int end);
    }
}
