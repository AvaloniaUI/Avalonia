namespace Avalonia.Input
{
    public interface ITextInputHandler
    {
        void OnTextEntered(uint timestamp, string text);
    }
}
