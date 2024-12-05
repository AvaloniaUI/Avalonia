using Android.OS;
using AndroidX.Core.View.Accessibility;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace Avalonia.Android.Automation
{
    internal class SelectionItemNodeInfoProvider : NodeInfoProvider<ISelectionItemProvider>
    {
        public SelectionItemNodeInfoProvider(AutomationPeer peer, int virtualViewId) : base(peer, virtualViewId)
        {
        }

        public override bool PerformNodeAction(int action, Bundle? arguments)
        {
            switch (action)
            {
                case AccessibilityNodeInfoCompat.ActionSelect:
                    GetProvider().Select();
                    return true;
                default:
                    return false;
            }
        }

        public override void PopulateNodeInfo(AccessibilityNodeInfoCompat nodeInfo)
        {
            nodeInfo.AddAction(AccessibilityNodeInfoCompat.ActionSelect);
            nodeInfo.Selected = GetProvider().IsSelected;
        }
    }
}
