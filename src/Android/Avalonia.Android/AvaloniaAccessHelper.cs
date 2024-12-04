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
        private static readonly IReadOnlyDictionary<Type, NodeInfoProviderInitializer>
            s_providerTypeInitializers = new Dictionary<Type, NodeInfoProviderInitializer>()
            {
                { typeof(IEmbeddedRootProvider), (peer, id) => new EmbeddedRootNodeInfoProvider(peer, id) },
                { typeof(IInvokeProvider), (peer, id) => new InvokeNodeInfoProvider(peer, id) },
            };

        private readonly Dictionary<int, AutomationPeer> _peers;
        private readonly Dictionary<AutomationPeer, HashSet<INodeInfoProvider>> _peerNodeInfoProviders;

        private readonly AvaloniaView _view;

        public AvaloniaAccessHelper(AvaloniaView view) : base(view.TopLevelImpl.View)
        {
            _peers = [];
            _peerNodeInfoProviders = [];

            AutomationPeer rootPeer = ControlAutomationPeer.CreatePeerForElement(view.TopLevel!);
            GetOrCreateNodeInfoProvidersFromPeer(rootPeer, out int _);
            rootPeer.ChildrenChanged += (s, ev) => 
            {
                _peerNodeInfoProviders.Clear();
                _peers.Clear();

                InvalidateRoot();
            };

            _view = view;
        }

        private HashSet<INodeInfoProvider>? GetNodeInfoProvidersFromVirtualViewId(int virtualViewId)
        {
            if (_peers.TryGetValue(virtualViewId, out AutomationPeer? peer) &&
                _peerNodeInfoProviders.TryGetValue(peer, out HashSet<INodeInfoProvider>? nodeInfoProviders))
            {
                return nodeInfoProviders;
            }
            else
            {
                return null;
            }
        }

        private HashSet<INodeInfoProvider> GetOrCreateNodeInfoProvidersFromPeer(AutomationPeer peer, out int virtualViewId)
        {
            int peerViewId;
            if (_peerNodeInfoProviders.TryGetValue(peer, out HashSet<INodeInfoProvider>? nodeInfoProviders))
            {
                peerViewId = nodeInfoProviders
                    .Select(x => x.VirtualViewId)
                    .Distinct()
                    .Single();
            }
            else
            {
                nodeInfoProviders = new();
                peerViewId = _peerNodeInfoProviders.Count;

                _peers.Add(peerViewId, peer);
                _peerNodeInfoProviders.Add(peer, nodeInfoProviders);

                peer.PropertyChanged += (s, ev) => InvalidateVirtualView(peerViewId);

                Type peerType = peer.GetType();
                IEnumerable<Type> providerTypes = peerType.GetInterfaces()
                    .Where(x => x.Namespace == nameof(Avalonia.Automation.Provider));
                foreach (Type providerType in providerTypes)
                {
                    if (s_providerTypeInitializers.TryGetValue(providerType, out NodeInfoProviderInitializer? ctor))
                    {
                        INodeInfoProvider nodeInfoProvider = ctor(peer, peerViewId);
                        nodeInfoProviders.Add(nodeInfoProvider);
                    }
                }
            }

            virtualViewId = peerViewId;
            return nodeInfoProviders;
        }

        private NodeInfoProvider<IEmbeddedRootProvider>? GetHostNodeInfoProvider()
        {
            return GetNodeInfoProvidersFromVirtualViewId(HostId)
                ?.SingleOrDefault() as NodeInfoProvider<IEmbeddedRootProvider>;
        }

        protected override int GetVirtualViewAt(float x, float y)
        {
            AutomationPeer? peer = GetHostNodeInfoProvider()?
                .GetProvider().GetPeerFromPoint(new(x, y));
            if (peer is not null)
            {
                GetOrCreateNodeInfoProvidersFromPeer(peer, out int virtualViewId);
                return virtualViewId;
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

            foreach (AutomationPeer peer in _peers[HostId].GetChildren().Where(x => !x.IsOffscreen()))
            {
                GetOrCreateNodeInfoProvidersFromPeer(peer, out int virtualViewId);
                virtualViewIds.Add(Integer.ValueOf(virtualViewId));
            }
        }

        protected override bool OnPerformActionForVirtualView(int virtualViewId, int action, Bundle? arguments)
        {
            return (GetNodeInfoProvidersFromVirtualViewId(virtualViewId) ?? [])
                .Select(x => x.PerformNodeAction(action, arguments))
                .Aggregate(false, (a, b) => a | b);
        }

        protected override void OnPopulateNodeForVirtualView(int virtualViewId, AccessibilityNodeInfoCompat nodeInfo)
        {
            if (!_peers.TryGetValue(virtualViewId, out AutomationPeer? peer))
            {
                return;
            }

            nodeInfo.Text = peer.GetName();
            nodeInfo.ClassName = peer.GetClassName();
            nodeInfo.Enabled = peer.IsEnabled();
            nodeInfo.Focusable = peer.IsKeyboardFocusable();
            nodeInfo.HintText = peer.GetHelpText();

            AutomationPeer? labeledBy = peer.GetLabeledBy();
            if (labeledBy is not null)
            {
                GetOrCreateNodeInfoProvidersFromPeer(labeledBy, out int labeledById);
                nodeInfo.SetLabeledBy(_view.TopLevelImpl.View, labeledById);
            }

            Rect bounds = peer.GetBoundingRectangle();
            PixelRect screenRect = new(
                _view.TopLevelImpl.PointToScreen(bounds.TopLeft),
                _view.TopLevelImpl.PointToScreen(bounds.BottomRight)
                );
            nodeInfo.SetBoundsInScreen(new(
                screenRect.X, screenRect.Y, 
                screenRect.Right, screenRect.Bottom
                ));

            foreach (INodeInfoProvider nodeInfoProvider in _peerNodeInfoProviders[peer])
            {
                nodeInfoProvider.PopulateNodeInfo(nodeInfo);
            }
        }
    }
}
