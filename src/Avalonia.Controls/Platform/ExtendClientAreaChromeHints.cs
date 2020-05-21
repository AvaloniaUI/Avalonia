using System;

namespace Avalonia.Platform
{
    [Flags]
    public enum ExtendClientAreaChromeHints
    {
        NoChrome,
        Default = SystemTitleBar | SystemChromeButtons,        
        SystemTitleBar = 0x01,
        SystemChromeButtons = 0x02,
        ManagedChromeButtons = 0x04,
        PreferSystemChromeButtons = 0x08,
        AdaptiveChromeWithTitleBar = SystemTitleBar | PreferSystemChromeButtons,
        AdaptiveChromeWithoutTitleBar = PreferSystemChromeButtons,
    }
}
