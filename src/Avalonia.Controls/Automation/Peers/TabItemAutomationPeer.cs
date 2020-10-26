using System.Collections.Generic;

#nullable enable

namespace Avalonia.Controls.Automation.Peers
{
    public class TabItemAutomationPeer : ContentControlAutomationPeer
    {
        public TabItemAutomationPeer(Control owner) : base(owner) { }

        protected override IReadOnlyList<AutomationPeer>? GetChildrenCore()
        {
            var tabItem = Owner as TabItem;

            if (tabItem?.IsSelected == true &&
                tabItem.Parent is TabControl tabControl &&
                tabControl.ContentPart?.Child is object)
            {
                return GetChildren(tabControl.ContentPart.Child);
            }

            return null;
        }
    }
}
