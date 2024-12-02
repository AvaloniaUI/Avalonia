using Android.OS;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace Avalonia.Android.Automation
{
    public class EmbeddedRootNodeInfoProvider : NodeInfoProvider<IEmbeddedRootProvider>
    {
        public EmbeddedRootNodeInfoProvider(AutomationPeer peer, int virtualViewId) : base(peer, virtualViewId)
        {
        }

        public override bool PerformNodeAction(int action, Bundle arguments) => false;
    }
}
