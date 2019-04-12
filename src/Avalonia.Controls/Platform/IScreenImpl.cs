using System.Collections.Generic;

namespace Avalonia.Platform
{
    public interface IScreenImpl
    {
        int ScreenCount { get; }

        IReadOnlyList<Screen> AllScreens { get; }
    }
}
