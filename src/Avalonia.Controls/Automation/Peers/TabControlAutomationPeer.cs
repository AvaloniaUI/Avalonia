using Avalonia.Controls;

#nullable enable

namespace Avalonia.Automation.Peers
{
    public class TabControlAutomationPeer : SelectingItemsControlAutomationPeer
    {
        public TabControlAutomationPeer(TabControl owner)
            : base(owner) 
        {
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Tab;
        }
    }
}
