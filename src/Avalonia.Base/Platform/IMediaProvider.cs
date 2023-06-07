using System;

namespace Avalonia.Platform
{
    public interface IMediaProvider
    {
        double GetScreenWidth();

        double GetScreenHeight();

        DeviceOrientation GetDeviceOrientation();

        event EventHandler? ScreenSizeChanged;
        event EventHandler? OrientationChanged;
    }
}
