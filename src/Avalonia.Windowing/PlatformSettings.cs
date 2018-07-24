using System;
using Avalonia.Platform;

namespace Avalonia.Windowing
{
    public class PlatformSettings : IPlatformSettings
    {
        public Size DoubleClickSize => new Size(4, 4);
        public TimeSpan DoubleClickTime => TimeSpan.FromMilliseconds(200);
    }
}
