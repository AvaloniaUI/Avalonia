namespace Avalonia.Platform
{
    public interface IScreenImpl
    {
        int ScreenCount { get; }

        IScreenImpl[] AllScreens { get; }

        IScreenImpl PrimaryScreen { get; }

        Rect Bounds { get; }

        Rect WorkingArea { get; }

        bool Primary { get; }
    }
}