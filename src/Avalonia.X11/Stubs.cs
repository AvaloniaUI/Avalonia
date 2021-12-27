using System;
using Avalonia.Platform;

namespace Avalonia.X11
{
    class PlatformSettingsStub : IPlatformSettings, ITouchPlatformSettings
    {
        public Size DoubleClickSize { get; } = new Size(2, 2);
        public TimeSpan DoubleClickTime { get; } = TimeSpan.FromMilliseconds(500);

        public Size TouchDoubleClickSize => new Size(16, 16);

        public TimeSpan TouchDoubleClickTime => DoubleClickTime;
    }
}
