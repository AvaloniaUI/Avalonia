using Avalonia.Automation.Peers;
using Avalonia.Automation.Platform;
using Avalonia.Automation.Provider;
using Avalonia.Threading;

#nullable enable

namespace Avalonia.Win32.Automation
{
    internal class AutomationNodeFactory : IAutomationNodeFactory
    {
        public static readonly AutomationNodeFactory Instance = new AutomationNodeFactory();

        public IAutomationNode CreateNode(AutomationPeer peer)
        {
            Dispatcher.UIThread.VerifyAccess();
            return peer is IRootProvider ? new RootAutomationNode(peer) : new AutomationNode(peer);
        }
    }
}
