using System.Collections.Generic;
using Avalonia.Controls;

namespace Avalonia.Automation.Peers
{
    public class ListBoxAutomationPeer : SelectingItemsControlAutomationPeer
    {
        public ListBoxAutomationPeer(ListBox owner)
            : base(owner)
        {
        }

        public new ListBox Owner => (ListBox)base.Owner;

        protected override IReadOnlyList<AutomationPeer>? GetChildrenCore()
        {
            if (Owner.ItemCount == 0)
            {
                return null;
            }

            var children = new List<AutomationPeer>();

            for (var i = 0; i < Owner.ItemCount; i++)
            {
                var container = Owner.ContainerFromIndex(i);

                if (container == null)
                {
                    children.Add(new VirtualListItemAutomationPeer(Owner, i));
                }
                else
                {
                    children.Add(GetOrCreate(container));
                }
            }

            return children;
        }
    }
}
