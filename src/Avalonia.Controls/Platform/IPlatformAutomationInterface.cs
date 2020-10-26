using Avalonia.Controls.Automation.Peers;
using Avalonia.Platform;

#nullable enable

namespace Avalonia.Controls.Platform
{
    public interface IPlatformAutomationInterface
    {
        IAutomationPeerImpl CreateAutomationPeerImpl(AutomationPeer peer);
    }
}
