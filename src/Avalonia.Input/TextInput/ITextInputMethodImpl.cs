namespace Avalonia.Input.TextInput
{
    public interface ITextInputMethodImpl
    {
        void SetActive(bool active);
        void SetCursorRect(Rect rect);
        void SetOptions(TextInputOptionsQueryEventArgs options);
    }
    
    public interface ITextInputMethodRoot : IInputRoot
    {
        ITextInputMethodImpl InputMethod { get; }
    }
}
