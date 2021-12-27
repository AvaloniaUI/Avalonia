using System;

namespace Avalonia.Platform
{
    public interface ITouchPlatformSettings
    {
        Size TouchDoubleClickSize { get; }

        TimeSpan TouchDoubleClickTime { get; }
    }
}
