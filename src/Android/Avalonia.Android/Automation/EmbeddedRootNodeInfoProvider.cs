using Android.OS;
using AndroidX.Core.View.Accessibility;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace Avalonia.Android.Automation
{
    public class EmbeddedRootNodeInfoProvider : NodeInfoProvider<IEmbeddedRootProvider>
    {
        public EmbeddedRootNodeInfoProvider(AutomationPeer peer, int virtualViewId) : base(peer, virtualViewId)
        {
        }

        public override bool PerformNodeAction(int action, Bundle? arguments) => false;

        public override void PopulateNodeInfo(AccessibilityNodeInfoCompat nodeInfo, bool invokeDefault)
        {
            PopulateNodeInfo(nodeInfo);
        }
    }
}
