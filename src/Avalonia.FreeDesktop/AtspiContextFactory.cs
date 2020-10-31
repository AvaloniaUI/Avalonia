using Avalonia.Controls;
using Avalonia.Controls.Automation.Peers;
using Avalonia.FreeDesktop.Atspi;
using Avalonia.Threading;

namespace Avalonia.FreeDesktop
{
    internal static class AtspiContextFactory
    {
        public static AtspiContext Create(AtspiRoot root, AutomationPeer peer)
        {
            Dispatcher.UIThread.VerifyAccess();

            if (peer.PlatformImpl is object)
            {
                throw new AvaloniaInternalException($"Peer already has a platform implementation: {peer}.");
            }

            var result = peer switch
            {
                ButtonAutomationPeer _ => new AtspiContext(root, peer, AtspiRole.ATSPI_ROLE_PUSH_BUTTON),
                MenuAutomationPeer _ => new AtspiContext(root, peer, AtspiRole.ATSPI_ROLE_MENU),
                MenuItemAutomationPeer _ => new AtspiContext(root, peer, AtspiRole.ATSPI_ROLE_MENU_ITEM),
                TabControlAutomationPeer _ => new AtspiContext(root, peer, AtspiRole.ATSPI_ROLE_PAGE_TAB_LIST),
                TabItemAutomationPeer _ => new AtspiContext(root, peer, AtspiRole.ATSPI_ROLE_PAGE_TAB),
                TextAutomationPeer _ => new AtspiContext(root, peer, AtspiRole.ATSPI_ROLE_STATIC),
                _ => new AtspiContext(root, peer, AtspiRole.ATSPI_ROLE_UNKNOWN),
            };

            //var _ = result.Update();
            return result;
        }
    }
}
