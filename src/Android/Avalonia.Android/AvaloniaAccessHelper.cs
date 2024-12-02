using System;
using System.Collections.Generic;
using System.Linq;
using Android.OS;
using AndroidX.Core.View.Accessibility;
using AndroidX.CustomView.Widget;
using Avalonia.Android.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Java.Lang;

namespace Avalonia.Android
{
    internal class AvaloniaAccessHelper : ExploreByTouchHelper
    {
        private static readonly IReadOnlyDictionary<Type, Func<AutomationPeer, int, INodeInfoProvider>> s_nodeInfoProviderTypes = 
            new Dictionary<Type, Func<AutomationPeer, int, INodeInfoProvider>>() 
            {
                { typeof(IEmbeddedRootProvider), (peer, id) => new EmbeddedRootNodeInfoProvider(peer, id) },
                { typeof(IInvokeProvider), (peer, id) => new InvokeNodeInfoProvider(peer, id) },
            };

        private readonly Dictionary<int, AutomationPeer> _peers;
        private readonly Dictionary<AutomationPeer, INodeInfoProvider> _nodeInfoProviders;

        private readonly AvaloniaView _view;

        public AvaloniaAccessHelper(AvaloniaView view) : base(view.TopLevelImpl.View)
        {
            _peers = [];
            _nodeInfoProviders = [];

            AutomationPeer rootPeer = ControlAutomationPeer.CreatePeerForElement(view.TopLevel!);
            GetOrCreateNodeInfoProviderFromPeer(rootPeer);

            _view = view;
        }

        private INodeInfoProvider? GetNodeInfoProviderFromVirtualViewId(int virtualViewId)
        {
            if (_peers.TryGetValue(virtualViewId, out AutomationPeer? peer) &&
                _nodeInfoProviders.TryGetValue(peer, out INodeInfoProvider? nodeInfoProvider))
            {
                return nodeInfoProvider;
            }
            else
            {
                return null;
            }
        }

        private INodeInfoProvider GetOrCreateNodeInfoProviderFromPeer(AutomationPeer peer) 
        {
            if (!_nodeInfoProviders.TryGetValue(peer, out INodeInfoProvider? nodeInfoProvider))
            {
                Type peerType = peer.GetType();
                Type peerInterfaceType = peerType.GetInterfaces().Single();

                nodeInfoProvider = s_nodeInfoProviderTypes[peerInterfaceType](peer, _nodeInfoProviders.Count);

                _peers.Add(nodeInfoProvider.VirtualViewId, peer);
                _nodeInfoProviders.Add(peer, nodeInfoProvider);
            }

            return nodeInfoProvider;
        }

        private NodeInfoProvider<IEmbeddedRootProvider>? GetHostNodeInfoProvider() 
        {
            return GetNodeInfoProviderFromVirtualViewId(HostId) as NodeInfoProvider<IEmbeddedRootProvider>;
        }

        protected override int GetVirtualViewAt(float x, float y)
        {
            AutomationPeer? peer = GetHostNodeInfoProvider()?
                .GetProvider().GetPeerFromPoint(new(x, y));
            if (peer is not null)
            {
                return GetOrCreateNodeInfoProviderFromPeer(peer).VirtualViewId;
            }
            else
            {
                return InvalidId;
            }
        }

        protected override void GetVisibleVirtualViews(IList<Integer>? virtualViewIds)
        {
            if (virtualViewIds is null)
            {
                return;
            }

            foreach (AutomationPeer peer in _peers[HostId].GetChildren())
            {
                INodeInfoProvider nodeInfoProvider = GetOrCreateNodeInfoProviderFromPeer(peer);
                virtualViewIds.Add(Integer.ValueOf(nodeInfoProvider.VirtualViewId));
            }
        }

        protected override bool OnPerformActionForVirtualView(int virtualViewId, int action, Bundle? arguments)
        {
            return GetNodeInfoProviderFromVirtualViewId(virtualViewId)?
                .PerformNodeAction(action, arguments) ?? false;
        }

        protected override void OnPopulateNodeForVirtualView(int virtualViewId, AccessibilityNodeInfoCompat nodeInfo)
        {
            AutomationPeer peer = _peers[virtualViewId];
            INodeInfoProvider nodeInfoProvider = _nodeInfoProviders[peer];

            nodeInfoProvider.PopulateNodeInfo(nodeInfo);

            Rect bounds = peer.GetBoundingRectangle();
            PixelRect screenRect = new(
                _view.TopLevelImpl.PointToScreen(bounds.TopLeft),
                _view.TopLevelImpl.PointToScreen(bounds.BottomRight)
                );
            nodeInfo.SetBoundsInScreen(new(screenRect.X, screenRect.Y, screenRect.Right, screenRect.Bottom));
        }
    }
}
