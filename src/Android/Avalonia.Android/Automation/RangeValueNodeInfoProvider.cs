using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            IRangeValueProvider rangeValueProvider = GetProvider();
            nodeInfo.RangeInfo = new AccessibilityNodeInfoCompat.RangeInfoCompat(
                AccessibilityNodeInfoCompat.RangeInfoCompat.RangeTypeFloat, 
                (float)rangeValueProvider.Minimum, (float)rangeValueProvider.Maximum, 
                (float)rangeValueProvider.Value
                );
        }
    }
}
