using Android.OS;
using AndroidX.Core.View.Accessibility;
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
            switch (action)
            {
                case AccessibilityNodeInfoCompat.ActionExpand:
                    GetProvider().Expand();
                    return true;
                case AccessibilityNodeInfoCompat.ActionCollapse:
                    GetProvider().Collapse();
                    return true;
                default:
                    return false;
            }
        }

        public override void PopulateNodeInfo(AccessibilityNodeInfoCompat nodeInfo)
        {
            nodeInfo.AddAction(AccessibilityNodeInfoCompat.ActionExpand);
            nodeInfo.AddAction(AccessibilityNodeInfoCompat.ActionCollapse);
        }
    }
}
