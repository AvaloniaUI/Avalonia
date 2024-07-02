using System;
using Avalonia.Styling;

namespace Avalonia.Platform
{
    public interface IMediaProvider
    {
        string GetPlatform();
        double GetScreenWidth();

        double GetScreenHeight();

        MediaOrientation GetDeviceOrientation();

        event EventHandler? ScreenSizeChanged;
        event EventHandler? OrientationChanged;
    }
}
