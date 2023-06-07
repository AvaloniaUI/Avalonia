using System;

namespace Avalonia.Platform
{
    public interface IMediaProvider
    {
        string GetPlatform();
        double GetScreenWidth();

        double GetScreenHeight();

        DeviceOrientation GetDeviceOrientation();

        event EventHandler? ScreenSizeChanged;
        event EventHandler? OrientationChanged;
    }
}
