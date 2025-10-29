using System.Collections.Generic;
using System.Linq;
using Android.OS;
using AndroidX.Core.View.Accessibility;
using AndroidX.CustomView.Widget;
using Avalonia.Android.Automation;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Java.Lang;

namespace Avalonia.Android
{
    internal class AvaloniaAccessHelper : ExploreByTouchHelper
    {
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
            GetOrCreateNodeInfoProvidersFromPeer(rootPeer, out int _);

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

                peer.ChildrenChanged += (s, ev) => InvalidateVirtualView(peerViewId,
                    AccessibilityEventCompat.ContentChangeTypeSubtree);
                peer.PropertyChanged += (s, ev) =>
                {
                    if (ev.Property == AutomationElementIdentifiers.NameProperty)
                    {
                        InvalidateVirtualView(peerViewId, AccessibilityEventCompat.ContentChangeTypeText);
                    }
                    else if (ev.Property == AutomationElementIdentifiers.HelpTextProperty)
                    {
                        InvalidateVirtualView(peerViewId, AccessibilityEventCompat.ContentChangeTypeContentDescription);
                    }
                    else if (ev.Property == AutomationElementIdentifiers.BoundingRectangleProperty || 
                        ev.Property == AutomationElementIdentifiers.ClassNameProperty)
                    {
                        InvalidateVirtualView(peerViewId);
                    }
                };

                if (peer is IExpandCollapseProvider)
                    nodeInfoProviders.Add(new ExpandCollapseNodeInfoProvider(this, peer, peerViewId));
                if (peer is IInvokeProvider)
                    nodeInfoProviders.Add(new InvokeNodeInfoProvider(this, peer, peerViewId));
                if (peer is IRangeValueProvider)
                    nodeInfoProviders.Add(new RangeValueNodeInfoProvider(this, peer, peerViewId));
                if (peer is IScrollProvider)
                    nodeInfoProviders.Add(new ScrollNodeInfoProvider(this, peer, peerViewId));
                if (peer is ISelectionItemProvider)
                    nodeInfoProviders.Add(new SelectionItemNodeInfoProvider(this, peer, peerViewId));
                if (peer is IToggleProvider)
                    nodeInfoProviders.Add(new ToggleNodeInfoProvider(this, peer, peerViewId));
                if (peer is IValueProvider)
                    nodeInfoProviders.Add(new ValueNodeInfoProvider(this, peer, peerViewId));
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

        protected override void OnPopulateNodeForVirtualView(int virtualViewId, AccessibilityNodeInfoCompat? nodeInfo)
        {
            if (nodeInfo is null || !_peers.TryGetValue(virtualViewId, out AutomationPeer? peer))
            {
                return; // BAIL!! No work to be done
            }

            // UI logical structure
            foreach (AutomationPeer child in peer.GetChildren())
            {
                GetOrCreateNodeInfoProvidersFromPeer(child, out int childId);
                nodeInfo.AddChild(_view, childId);
            }

            // UI labels
            AutomationPeer? labeledBy = peer.GetLabeledBy();
            if (labeledBy is not null)
            {
                GetOrCreateNodeInfoProvidersFromPeer(labeledBy, out int labeledById);
                nodeInfo.SetLabeledBy(_view, labeledById);
            }

            // UI debug metadata
            nodeInfo.ClassName = peer.GetClassName();
            nodeInfo.UniqueId = peer.GetAutomationId();

            // Common control state
            nodeInfo.Enabled = peer.IsEnabled();

            // Control focus state
            bool canFocusAtAll = peer.IsContentElement() && !peer.IsOffscreen();
            nodeInfo.ScreenReaderFocusable = canFocusAtAll;
            nodeInfo.Focusable = canFocusAtAll && peer.IsKeyboardFocusable();

            nodeInfo.AccessibilityFocused = peer.HasKeyboardFocus();
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
            nodeInfo.Text ??= peer.GetName();
            nodeInfo.ContentDescription ??= peer.GetHelpText();
        }
    }
}
