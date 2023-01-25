using System.Collections.Generic;
using Avalonia.Metadata;

namespace Avalonia.Platform
{
    [Unstable]
    public interface IScreenImpl
    {
        /// <summary>
        /// Gets the total number of screens available on the device.
        /// </summary>
        int ScreenCount { get; }

        /// <summary>
        /// Gets the list of all screens available on the device.
        /// </summary>
        IReadOnlyList<Screen> AllScreens { get; }

        Screen? ScreenFromWindow(IWindowBaseImpl window);

        Screen? ScreenFromPoint(PixelPoint point);

        Screen? ScreenFromRect(PixelRect rect);
    }
}
