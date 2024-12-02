using System;
using System.Linq;
using Android.OS;
using AndroidX.Core.View.Accessibility;
using Avalonia.Automation.Peers;

namespace Avalonia.Android.Automation
{
    public abstract class NodeInfoProvider<T> : INodeInfoProvider
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

        public abstract bool PerformNodeAction(int action, Bundle arguments);

        public virtual void PopulateNodeInfo(AccessibilityNodeInfoCompat nodeInfo) 
        {
            nodeInfo.ClassName = _peer.GetClassName();
            nodeInfo.Enabled = _peer.IsEnabled();
            nodeInfo.Focusable = _peer.IsKeyboardFocusable();
            nodeInfo.HintText = _peer.GetHelpText();
        }
    }
}
