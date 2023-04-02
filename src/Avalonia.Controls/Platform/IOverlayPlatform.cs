using System;
using Avalonia.Metadata;

namespace Avalonia.Platform
{
    [Unstable]
    public interface IOverlayPlatform
    {
        IWindowImpl CreateOverlay(IntPtr parentWindow, string parentView);
    }

    public interface IOverlayUmbilical
    {
        void LogFirstResponser(string firstResponder);

        bool ShouldPassThrough(Point point);
    }
}
