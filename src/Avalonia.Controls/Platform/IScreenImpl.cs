using System.Collections.Generic;

#nullable enable

namespace Avalonia.Platform
{
    public interface IScreenImpl
    {
        int ScreenCount { get; }

        IReadOnlyList<Screen> AllScreens { get; }

        Screen? ScreenFromWindow(IWindowBaseImpl window);

        Screen? ScreenFromPoint(PixelPoint point);

        Screen? ScreenFromRect(PixelRect rect);
    }
}
