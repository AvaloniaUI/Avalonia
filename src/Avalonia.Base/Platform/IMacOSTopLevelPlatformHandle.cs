using System;

namespace Avalonia.Platform
{
    public interface IMacOSTopLevelPlatformHandle
    {
        IntPtr NSView { get; }
        IntPtr GetNSViewRetained();
        IntPtr NSWindow { get; }
        IntPtr GetNSWindowRetained();
    }
}
