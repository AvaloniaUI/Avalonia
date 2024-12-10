using System;
using Android.OS;
using AndroidX.Core.View.Accessibility;
using Avalonia.Automation.Peers;

namespace Avalonia.Android.Automation
{
    internal delegate INodeInfoProvider NodeInfoProviderInitializer(AutomationPeer peer, int virtualViewId);

    internal abstract class NodeInfoProvider<T> : INodeInfoProvider
    {
        private readonly AutomationPeer _peer;

        public NodeInfoProvider(AutomationPeer peer, int virtualViewId)
        {
            _peer = peer;
            VirtualViewId = virtualViewId;
        }

        public int VirtualViewId { get; }

        public T GetProvider() => _peer.GetProvider<T>() ??
            throw new InvalidOperationException($"Peer instance does not implement {nameof(T)}.");

        public abstract bool PerformNodeAction(int action, Bundle? arguments);

        public abstract void PopulateNodeInfo(AccessibilityNodeInfoCompat nodeInfo);
    }
}
