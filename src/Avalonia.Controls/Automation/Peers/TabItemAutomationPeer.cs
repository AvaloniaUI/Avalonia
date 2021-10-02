using Avalonia.Controls;

namespace Avalonia.Automation.Peers
{
    public class TabItemAutomationPeer : ListItemAutomationPeer
    {
        public TabItemAutomationPeer(TabItem owner)
            : base(owner)
        {
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.TabItem;
        }
    }
}
