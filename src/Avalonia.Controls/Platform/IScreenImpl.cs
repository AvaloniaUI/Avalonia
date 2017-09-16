namespace Avalonia.Platform
{
    public interface IScreenImpl
    {
        int ScreenCount { get; }

        Screen[] AllScreens { get; }
    }
}