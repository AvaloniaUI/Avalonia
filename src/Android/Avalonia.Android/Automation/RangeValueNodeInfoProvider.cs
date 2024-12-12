using Android.OS;
using AndroidX.Core.View.Accessibility;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace Avalonia.Android.Automation
{
    internal class RangeValueNodeInfoProvider : NodeInfoProvider<IRangeValueProvider>
    {
        public RangeValueNodeInfoProvider(AutomationPeer peer, int virtualViewId) : base(peer, virtualViewId)
        {
        }

        public override bool PerformNodeAction(int action, Bundle? arguments)
        {
            return false;
        }

        public override void PopulateNodeInfo(AccessibilityNodeInfoCompat nodeInfo)
        {
            IRangeValueProvider provider = GetProvider();
            nodeInfo.RangeInfo = new AccessibilityNodeInfoCompat.RangeInfoCompat(
                AccessibilityNodeInfoCompat.RangeInfoCompat.RangeTypeFloat, 
                (float)provider.Minimum, (float)provider.Maximum, 
                (float)provider.Value
                );
        }
    }
}
