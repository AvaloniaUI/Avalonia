using Avalonia.Controls;

namespace Avalonia.Automation.Peers
{
    public class ListBoxAutomationPeer : SelectingItemsControlAutomationPeer
    {
        public ListBoxAutomationPeer(ListBox owner)
            : base(owner)
        {
        }
    }
}
