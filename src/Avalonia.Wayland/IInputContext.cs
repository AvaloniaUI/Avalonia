namespace Avalonia.Wayland
{
    internal interface IInputContext
    {
        bool HandleEvent(WlWindow window, ref KeyboardInputState state);
    }
}
