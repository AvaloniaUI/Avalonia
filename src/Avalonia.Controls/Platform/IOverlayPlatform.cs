using System;
using Avalonia.Metadata;

namespace Avalonia.Platform
{
    [Unstable]
    public interface IOverlayPlatform
    {
        IWindowImpl CreateOverlay(IntPtr parentWindow, string parentView);
    }

}
