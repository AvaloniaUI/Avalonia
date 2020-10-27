using System;
using Avalonia.Controls;
using Avalonia.Controls.Automation.Peers;
using Avalonia.Threading;
using Avalonia.Win32.Interop.Automation;

#nullable enable

namespace Avalonia.Win32.Automation
{
    internal static class AutomationProviderFactory
    {
        public static AutomationProvider Create(AutomationPeer peer)
        {
            Dispatcher.UIThread.VerifyAccess();

            if (peer.PlatformImpl is object)
            {
                throw new AvaloniaInternalException($"Peer already has a platform implementation: {peer}.");
            }

            if (peer is WindowAutomationPeer windowPeer)
            {
                var windowImpl = (WindowImpl)((Window)windowPeer.Owner).PlatformImpl;
                return new WindowProvider(windowImpl, windowPeer);
            }

            var result = peer switch
            {
                ButtonAutomationPeer _ => new AutomationProvider(peer, UiaControlTypeId.Button, true),
                MenuAutomationPeer _ => new AutomationProvider(peer, UiaControlTypeId.Menu, false),
                MenuItemAutomationPeer _ => new AutomationProvider(peer, UiaControlTypeId.MenuItem, true),
                TabControlAutomationPeer _ => new AutomationProvider(peer, UiaControlTypeId.Tab, true),
                TabItemAutomationPeer _ => new AutomationProvider(peer, UiaControlTypeId.TabItem, true),
                TextAutomationPeer _ => new AutomationProvider(peer, UiaControlTypeId.Text, true),
                _ => new AutomationProvider(peer, UiaControlTypeId.Custom, true),
            };

            var _ = result.Update();
            return result;
        }
    }
}
