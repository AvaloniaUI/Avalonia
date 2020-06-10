using System;

namespace Avalonia.Platform
{
    [Flags]
    public enum ExtendClientAreaChromeHints
    {
        NoChrome,
        Default = SystemTitleBar,        
        SystemTitleBar = 0x01,
        ManagedChromeButtons = 0x02,
        SystemChromeButtons = 0x04,

        OSXThickTitleBar = 0x08,

        PreferSystemChromeButtons = 0x10,
        
        AdaptiveChromeWithTitleBar = SystemTitleBar | PreferSystemChromeButtons,
        AdaptiveChromeWithoutTitleBar = PreferSystemChromeButtons,
    }
}
