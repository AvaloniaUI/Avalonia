using System;

namespace Avalonia.Platform
{
    [Flags]
    public enum ExtendClientAreaChromeHints
    {
        NoChrome,
        Default = SystemChrome,        
        SystemChrome = 0x01,
        PreferSystemChrome = 0x02,
        OSXThickTitleBar = 0x08,               
    }
}
