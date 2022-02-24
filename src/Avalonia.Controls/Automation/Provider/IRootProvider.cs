using System;
using Avalonia.Automation.Peers;
using Avalonia.Platform;

namespace Avalonia.Automation.Provider
{
    public interface IRootProvider
    {
        ITopLevelImpl? PlatformImpl { get; }
        AutomationPeer? GetFocus();
        AutomationPeer? GetPeerFromPoint(Point p);
        event EventHandler? FocusChanged;
    }
}
