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
        PreferSystemChromeButtons = 0x04,

        OSXThickTitleBar = 0x08,
        
        AdaptiveChromeWithTitleBar = SystemTitleBar | PreferSystemChromeButtons,
        AdaptiveChromeWithoutTitleBar = PreferSystemChromeButtons,
    }
}
