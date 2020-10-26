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

            var parent = peer.GetParent()?.PlatformImpl as AutomationProvider;

            if (parent is null)
            {
                throw new AvaloniaInternalException($"Could not find parent automation peer for {peer}.");
            }

            var result = peer switch
            {
                ButtonAutomationPeer _ => new AutomationProvider(parent, peer, UiaControlTypeId.Button, true),
                MenuAutomationPeer _ => new AutomationProvider(parent, peer, UiaControlTypeId.Menu, false),
                MenuItemAutomationPeer _ => new AutomationProvider(parent, peer, UiaControlTypeId.MenuItem, true),
                TabControlAutomationPeer _ => new AutomationProvider(parent, peer, UiaControlTypeId.Tab, true),
                TabItemAutomationPeer _ => new AutomationProvider(parent, peer, UiaControlTypeId.TabItem, true),
                TextAutomationPeer _ => new AutomationProvider(parent, peer, UiaControlTypeId.Text, true),
                _ => new AutomationProvider(parent, peer, UiaControlTypeId.Custom, true),
            };

            var _ = result.Update();
            return result;
        }
    }
}
