using System.Collections.Generic;

#nullable enable

namespace Avalonia.Controls.Automation.Peers
{
    public class ButtonAutomationPeer : ContentControlAutomationPeer
    {
        public ButtonAutomationPeer(Control owner): base(owner) {}
        protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() => null;
    }
}

