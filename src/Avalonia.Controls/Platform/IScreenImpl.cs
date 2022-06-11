using System.Collections.Generic;
using Avalonia.Metadata;

namespace Avalonia.Platform
{
    [Unstable]
    public interface IScreenImpl
    {
        int ScreenCount { get; }

        IReadOnlyList<Screen> AllScreens { get; }

        Screen? ScreenFromWindow(IWindowBaseImpl window);

        Screen? ScreenFromPoint(PixelPoint point);

        Screen? ScreenFromRect(PixelRect rect);
    }
}
