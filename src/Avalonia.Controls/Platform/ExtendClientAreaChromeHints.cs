using System;

namespace Avalonia.Platform
{
    [Flags]
    public enum ExtendClientAreaChromeHints
    {        
        Default = SystemTitleBar | SystemChromeButtons,
        NoChrome,
        SystemTitleBar = 0x01,
        SystemChromeButtons = 0x02,
        ManagedChromeButtons = 0x04,
        PreferSystemChromeButtons = 0x08,
        AdaptiveChromeWithTitleBar = SystemTitleBar | PreferSystemChromeButtons,
        AdaptiveChromeWithoutTitleBar = PreferSystemChromeButtons,
    }
}
