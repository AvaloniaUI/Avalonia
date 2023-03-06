using Avalonia.Metadata;
using System;

namespace Avalonia.Platform
{
    [Unstable]
    public interface IOverlayPlatform
    {
        IWindowImpl CreateOverlay(IntPtr parentWindow, string parentView);
    }
}
