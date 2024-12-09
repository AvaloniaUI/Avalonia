using System.Collections.Generic;
using Avalonia.Automation.Peers;

namespace Avalonia.Controls.Automation.Peers
{
    public class PanelAutomationPeer : ControlAutomationPeer
    {
        public PanelAutomationPeer(Control owner) : base(owner)
        {
        }

        public new Panel Owner => (Panel)base.Owner;

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Pane;
        }

        protected override IReadOnlyList<AutomationPeer>? GetChildrenCore()
        {
            var children = Owner.Children;

            if (children.Count == 0)
                return null;

            var result = new List<AutomationPeer>();

            foreach (var child in children)
            {
                if (child is Control c)
                {
                    var peer = GetOrCreate(c);
                    if (c.IsVisible)
                        result.Add(peer);
                }
            }

            return result;
        }
    }
}
