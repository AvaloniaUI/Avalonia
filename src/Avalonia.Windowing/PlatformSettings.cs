using System;
using Avalonia.Platform;

namespace Avalonia.Windowing
{
    public class PlatformSettings : IPlatformSettings
    {
        public Size DoubleClickSize => new Size(4, 4);

        // TODO: This needs to be read from winit somehow
        public TimeSpan DoubleClickTime => TimeSpan.FromMilliseconds(500);
    }
}
