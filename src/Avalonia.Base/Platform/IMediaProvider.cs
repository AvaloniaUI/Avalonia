using System;

namespace Avalonia.Platform
{
    public interface IMediaProvider
    {
        double GetScreenWidth();

        double GetScreenHeight();

        event EventHandler? ScreenSizeChanged;
    }
}
