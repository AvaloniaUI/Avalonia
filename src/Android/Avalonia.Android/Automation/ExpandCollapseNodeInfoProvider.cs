using Android.OS;
using AndroidX.Core.View;
using AndroidX.Core.View.Accessibility;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace Avalonia.Android.Automation
{
    internal class ExpandCollapseNodeInfoProvider : NodeInfoProvider<IExpandCollapseProvider>
    {
        public ExpandCollapseNodeInfoProvider(AutomationPeer peer, int virtualViewId) : base(peer, virtualViewId)
        {
        }

        public override bool PerformNodeAction(int action, Bundle? arguments)
        {
            IExpandCollapseProvider provider = GetProvider();
            switch (action)
            {
                case AccessibilityNodeInfoCompat.ActionExpand:
                    provider.Expand();
                    return true;
                case AccessibilityNodeInfoCompat.ActionCollapse:
                    provider.Collapse();
                    return true;
                default:
                    return false;
            }
        }

        public override void PopulateNodeInfo(AccessibilityNodeInfoCompat nodeInfo)
        {
            nodeInfo.AddAction(AccessibilityNodeInfoCompat.ActionExpand);
            nodeInfo.AddAction(AccessibilityNodeInfoCompat.ActionCollapse);

            IExpandCollapseProvider provider = GetProvider();
            nodeInfo.StateDescription = provider.ExpandCollapseState switch
            {
                ExpandCollapseState.Expanded => provider.ShowsMenu ? "Menu, showing all menu items." : "List, showing all list items.",
                ExpandCollapseState.Collapsed => provider.ShowsMenu ? "Menu, menu items hidden." : "List, list items hidden.",
                ExpandCollapseState.PartiallyExpanded => provider.ShowsMenu ? "Menu, some items hidden." : "List, some items hidden.",
                ExpandCollapseState.LeafNode => provider.ShowsMenu ? "Menu, empty." : "List, empty.",
                _ => provider.ShowsMenu ? "Menu, undefined menu state." : "List, undefined list state."
            };
        }
    }
}
