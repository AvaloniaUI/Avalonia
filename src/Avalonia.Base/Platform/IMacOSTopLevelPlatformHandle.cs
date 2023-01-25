using System;
using Avalonia.Metadata;

namespace Avalonia.Platform
{
    [Unstable]
    public interface IMacOSTopLevelPlatformHandle
    {
        IntPtr NSView { get; }
        IntPtr GetNSViewRetained();
        IntPtr NSWindow { get; }
        IntPtr GetNSWindowRetained();
    }
}
