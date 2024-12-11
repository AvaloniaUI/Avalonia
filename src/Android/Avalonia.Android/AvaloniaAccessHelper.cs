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
        private static readonly IReadOnlyDictionary<string, NodeInfoProviderInitializer>
            s_providerTypeInitializers = new Dictionary<string, NodeInfoProviderInitializer>()
            {
                { typeof(IExpandCollapseProvider).FullName!, (peer, id) => new ExpandCollapseNodeInfoProvider(peer, id) },
                { typeof(IInvokeProvider).FullName!, (peer, id) => new InvokeNodeInfoProvider(peer, id) },
                { typeof(IRangeValueProvider).FullName!, (peer, id) => new RangeValueNodeInfoProvider(peer, id) },
                { typeof(IScrollProvider).FullName!, (peer, id) => new ScrollNodeInfoProvider(peer, id) },
                { typeof(ISelectionItemProvider).FullName!, (peer, id) => new SelectionItemNodeInfoProvider(peer, id) },
                { typeof(IToggleProvider).FullName!, (peer, id) => new ToggleNodeInfoProvider(peer, id) },
                { typeof(IValueProvider).FullName!, (peer, id) => new ValueNodeInfoProvider(peer, id) },
            };

        private readonly Dictionary<int, AutomationPeer> _peers;
        private readonly Dictionary<AutomationPeer, int> _peerIds;

        private readonly Dictionary<AutomationPeer, HashSet<INodeInfoProvider>> _peerNodeInfoProviders;

        private readonly AvaloniaView _view;

        public AvaloniaAccessHelper(AvaloniaView view) : base(view)
        {
            _peers = [];
            _peerIds = [];
            _peerNodeInfoProviders = [];

            AutomationPeer rootPeer = ControlAutomationPeer.CreatePeerForElement(view.TopLevel!);
            GetOrCreateNodeInfoProvidersFromPeer(rootPeer, out int rootId);
            rootPeer.ChildrenChanged += (s, ev) => InvalidateVirtualView(rootId, 
                AccessibilityEventCompat.ContentChangeTypeSubtree);
            rootPeer.PropertyChanged += (s, ev) => InvalidateVirtualView(rootId,
                AccessibilityEventCompat.ContentChangeTypeUndefined);

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
                peerViewId = _peerIds[peer];
            }
            else
            {
                peerViewId = _peerNodeInfoProviders.Count;
                _peers.Add(peerViewId, peer);
                _peerIds.Add(peer, peerViewId);

                nodeInfoProviders = new();
                _peerNodeInfoProviders.Add(peer, nodeInfoProviders);

                peer.PropertyChanged += (s, ev) => InvalidateVirtualView(peerViewId, 
                    AccessibilityEventCompat.ContentChangeTypeUndefined);
                peer.ChildrenChanged += (s, ev) => InvalidateVirtualView(peerViewId, 
                    AccessibilityEventCompat.ContentChangeTypeSubtree);

                Type peerType = peer.GetType();
                IEnumerable<Type> providerTypes = peerType.GetInterfaces()
                    .Where(x => x.Namespace == "Avalonia.Automation.Provider");
                foreach (Type providerType in providerTypes)
                {
                    if (s_providerTypeInitializers.TryGetValue(providerType.FullName!, out NodeInfoProviderInitializer? ctor))
                    {
                        INodeInfoProvider nodeInfoProvider = ctor(peer, peerViewId);
                        nodeInfoProviders.Add(nodeInfoProvider);
                    }
                }
            }

            virtualViewId = peerViewId;
            return nodeInfoProviders;
        }

        protected override int GetVirtualViewAt(float x, float y)
        {
            Point p = _view.TopLevelImpl.PointToClient(new PixelPoint((int)x, (int)y));
            IEmbeddedRootProvider? embeddedRootProvider = _peers[0].GetProvider<IEmbeddedRootProvider>();
            AutomationPeer? peer = embeddedRootProvider?.GetPeerFromPoint(p);
            if (peer is not null)
            {
                GetOrCreateNodeInfoProvidersFromPeer(peer, out int virtualViewId);
                return virtualViewId == 0 ? InvalidId : virtualViewId;
            }
            else
            {
                peer = embeddedRootProvider?.GetFocus();
                return peer is null ? InvalidId : _peerIds[peer];
            }
        }

        protected override void GetVisibleVirtualViews(IList<Integer>? virtualViewIds)
        {
            if (virtualViewIds is null)
            {
                return;
            }

            foreach (AutomationPeer peer in _peers[0].GetChildren())
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
                return; // BAIL!! No work to be done
            }

            // UI logical structure
            foreach (AutomationPeer child in peer.GetChildren())
            {
                GetOrCreateNodeInfoProvidersFromPeer(child, out int childId);
                nodeInfo.AddChild(_view, childId);
            }

            // UI labeling
            AutomationPeer? labeledBy = peer.GetLabeledBy();
            if (labeledBy is not null)
            {
                GetOrCreateNodeInfoProvidersFromPeer(labeledBy, out int labeledById);
                nodeInfo.SetLabeledBy(_view, labeledById);
            }

            // UI debug metadata
            nodeInfo.ClassName = peer.GetClassName();

            // Common control state
            nodeInfo.Enabled = peer.IsEnabled();

            // Control focus state
            bool canFocusAtAll = peer.IsContentElement() && !peer.IsOffscreen();
            nodeInfo.ScreenReaderFocusable = canFocusAtAll;

            nodeInfo.Focusable = canFocusAtAll && peer.IsKeyboardFocusable();
            nodeInfo.Focused = peer.HasKeyboardFocus();
            

            // On-screen bounds
            Rect bounds = peer.GetBoundingRectangle();
            PixelRect screenRect = new PixelRect(
                _view.TopLevelImpl.PointToScreen(bounds.TopLeft),
                _view.TopLevelImpl.PointToScreen(bounds.BottomRight)
                );
            nodeInfo.SetBoundsInParent(new(
                screenRect.X, screenRect.Y,
                screenRect.Right, screenRect.Bottom
                ));

            // UI provider specifics
            foreach (INodeInfoProvider nodeInfoProvider in _peerNodeInfoProviders[peer])
            {
                nodeInfoProvider.PopulateNodeInfo(nodeInfo);
            }

            // Control text contents
            nodeInfo.Text = nodeInfo.Text ?? peer.GetName();

            string helpText = peer.GetHelpText();
            if (helpText.Length > 0)
            {
                nodeInfo.ContentDescription = peer.GetHelpText();
            }
        }
    }
}
