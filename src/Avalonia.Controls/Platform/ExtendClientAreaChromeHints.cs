using System;

namespace Avalonia.Platform
{
    [Flags]
    public enum ExtendClientAreaChromeHints
    {
        NoChrome,
        Default = SystemTitleBar,        
        SystemTitleBar = 0x01,
        PreferSystemChromeButtons = 0x02,
        OSXThickTitleBar = 0x08,               
    }
}
