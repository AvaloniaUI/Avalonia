using System;
using System.Collections.Generic;

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
        private static readonly HashSet<AutomationControlType> s_containerTypes =
            new HashSet<AutomationControlType>()
            {
                AutomationControlType.Calendar,
                AutomationControlType.ComboBoxItem,
                AutomationControlType.Custom,
                AutomationControlType.DataGrid,
                AutomationControlType.DataItem,
                AutomationControlType.Document,
                AutomationControlType.Expander,
                AutomationControlType.Group,
                AutomationControlType.List,
                AutomationControlType.ListItem,
                AutomationControlType.Menu,
                AutomationControlType.MenuBar,
                AutomationControlType.MenuItem,
                AutomationControlType.None,
                AutomationControlType.Pane,
                AutomationControlType.ScrollViewer,
                AutomationControlType.SplitButton,
                AutomationControlType.Tab,
                AutomationControlType.TabItem,
                AutomationControlType.Table,
                AutomationControlType.TitleBar,
                AutomationControlType.ToolBar,
                AutomationControlType.Tree,
                AutomationControlType.TreeItem,
                AutomationControlType.Window,
            };

        private readonly Dictionary<int, AutomationPeer> _peers;
        private readonly Dictionary<AutomationPeer, int> _peerIds;

        private readonly Dictionary<AutomationPeer, HashSet<INodeInfoProvider>> _peerNodeInfoProviders;

        /// <remarks>
        /// Virtual view IDs must be allocated from a monotonic counter rather than derived from
        /// the size of <see cref="_peerNodeInfoProviders"/>: entries are now removed when their
        /// owner leaves the visual tree, so the dictionary's count no longer grows monotonically
        /// and reusing it would hand out an ID that is still in use.
        /// </remarks>
        private int _nextPeerViewId;

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
                peerViewId = _nextPeerViewId++;
                _peers.Add(peerViewId, peer);
                _peerIds.Add(peer, peerViewId);

                nodeInfoProviders = new();
                _peerNodeInfoProviders.Add(peer, nodeInfoProviders);

                EventHandler childrenChanged = (s, ev) => InvalidateVirtualView(peerViewId,
                    AccessibilityEventCompat.ContentChangeTypeSubtree);
                EventHandler<AutomationPropertyChangedEventArgs> propertyChanged = (s, ev) =>
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

                peer.ChildrenChanged += childrenChanged;
                peer.PropertyChanged += propertyChanged;

                // Drop the registration once the peer's control leaves the visual tree, otherwise
                // every control ever explored by accessibility is kept alive for the lifetime of
                // the view: the peer holds a strong reference to its Owner, and these three
                // dictionaries were never pruned. On a long-running app that rebuilds its UI (for
                // instance digital signage swapping screens), this retains each dead visual tree in
                // full — measured at ~2.5 MB per rebuild on a real device.
                // The root peer (ID 0) is deliberately never unregistered: GetVirtualViewAt and
                // GetVisibleVirtualViews index _peers[0] directly, so removing it would throw.
                // It is a single entry owned by the view itself, and dies with the helper.
                if (peerViewId != 0 && peer is ControlAutomationPeer controlPeer)
                {
                    EventHandler<VisualTreeAttachmentEventArgs>? detachedFromVisualTree = null;
                    detachedFromVisualTree = (s, ev) =>
                    {
                        controlPeer.Owner.DetachedFromVisualTree -= detachedFromVisualTree;
                        peer.ChildrenChanged -= childrenChanged;
                        peer.PropertyChanged -= propertyChanged;

                        _peers.Remove(peerViewId);
                        _peerIds.Remove(peer);
                        _peerNodeInfoProviders.Remove(peer);
                    };

                    controlPeer.Owner.DetachedFromVisualTree += detachedFromVisualTree;
                }

                if (peer.GetProvider<IExpandCollapseProvider>() is not null)
                    nodeInfoProviders.Add(new ExpandCollapseNodeInfoProvider(this, peer, peerViewId));
                if (peer.GetProvider<IInvokeProvider>() is not null)
                    nodeInfoProviders.Add(new InvokeNodeInfoProvider(this, peer, peerViewId));
                if (peer.GetProvider<IRangeValueProvider>() is not null)
                    nodeInfoProviders.Add(new RangeValueNodeInfoProvider(this, peer, peerViewId));
                if (peer.GetProvider<IScrollProvider>() is not null)
                    nodeInfoProviders.Add(new ScrollNodeInfoProvider(this, peer, peerViewId));
                if (peer.GetProvider<ISelectionItemProvider>() is not null)
                    nodeInfoProviders.Add(new SelectionItemNodeInfoProvider(this, peer, peerViewId));
                if (peer.GetProvider<IToggleProvider>() is not null)
                    nodeInfoProviders.Add(new ToggleNodeInfoProvider(this, peer, peerViewId));
                if (peer.GetProvider<IValueProvider>() is not null)
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
                int virtualViewId;
                if (peer.GetParent() is AutomationPeer parent && 
                    !s_containerTypes.Contains(parent.GetAutomationControlType()))
                {
                    GetOrCreateNodeInfoProvidersFromPeer(parent, out virtualViewId);
                }
                else
                {
                    GetOrCreateNodeInfoProvidersFromPeer(peer, out virtualViewId);
                }

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
            var providers = GetNodeInfoProvidersFromVirtualViewId(virtualViewId);
            if (providers == null)
            {
                return false;
            }

            var result = false;
            foreach (var provider in providers)
            {
                result |= TryPerformNodeAction(provider, action, arguments);
            }
            return result;
        }

        private static bool TryPerformNodeAction(INodeInfoProvider nodeInfoProvider, int action, Bundle? arguments)
        {
            try
            {
                return nodeInfoProvider.PerformNodeAction(action, arguments);
            }
            catch (ElementNotEnabledException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            catch (NotSupportedException)
            {
                return false;
            }
        }

        /// <summary>
        /// Placeholder used when a node would otherwise carry neither text nor content description.
        /// </summary>
        /// <remarks>
        /// A single space, deliberately: <c>ExploreByTouchHelper.createNodeForChild</c> rejects a
        /// node whose text and content description are both empty, and <c>TextUtils.isEmpty</c>
        /// treats <c>""</c> as empty - so an empty string does not satisfy the contract. A space
        /// does, without inventing a label that screen readers would announce.
        /// </remarks>
        private const string EmptyNodeDescription = " ";

        protected override void OnPopulateNodeForVirtualView(int virtualViewId, AccessibilityNodeInfoCompat? nodeInfo)
        {
            if (nodeInfo is null)
            {
                return; // BAIL!! No work to be done
            }

            if (!_peers.TryGetValue(virtualViewId, out AutomationPeer? peer))
            {
                // Stale ID: the peer was unregistered when its control left the visual tree,
                // but the platform can still ask for a node it obtained earlier - it caches
                // them, and it re-queries the accessibility focused one. Leaving the node
                // untouched is not an option: ExploreByTouchHelper.createNodeForChild
                // validates what the callback produced and throws "Callbacks must add text or
                // a content description in populateNodeForVirtualViewId()" when both are empty,
                // then again for the bounds - from inside an accessibility callback, which
                // takes the whole application down. Describe an inert node instead.
                nodeInfo.ContentDescription = EmptyNodeDescription;
                nodeInfo.Enabled = false;
                nodeInfo.Focusable = false;
                nodeInfo.ScreenReaderFocusable = false;
                nodeInfo.SetBoundsInScreen(new(0, 0, 0, 0));
                return;
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
                nodeInfo.AddLabeledBy(_view, labeledById);
            }

            // UI debug metadata
            nodeInfo.ClassName = peer.GetClassName();
            var automationId = peer.GetAutomationId();
            nodeInfo.UniqueId = automationId;
            nodeInfo.ViewIdResourceName = automationId;

            // Common control state
            nodeInfo.Enabled = peer.IsEnabled();

            // Control focus state
            bool canFocusAtAll = peer.IsControlElement() && !peer.IsOffscreen();
            nodeInfo.ScreenReaderFocusable = canFocusAtAll;
            nodeInfo.Focusable = canFocusAtAll && peer.IsKeyboardFocusable();

            nodeInfo.AccessibilityFocused = peer.HasKeyboardFocus();
            nodeInfo.Focused = peer.HasKeyboardFocus();

            // On-screen bounds
            Rect bounds = peer.GetBoundingRectangle();
            PixelRect screenRect = new PixelRect(
                _view.TopLevelImpl.InputRoot?.RootElement.PointToScreen(bounds.TopLeft) ?? default,
                _view.TopLevelImpl.InputRoot?.RootElement.PointToScreen(bounds.BottomRight) ?? default
                );
            nodeInfo.SetBoundsInScreen(new(
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

            // AutomationPeer.GetName()/GetHelpText() never return null - they collapse a missing
            // value to string.Empty - so the two assignments above always run, and they can both
            // assign an empty string. That happens for any peer that is a pure container: a Panel,
            // a Border, the TextSelectorLayer added when a text selection starts... Such a node is
            // rejected by ExploreByTouchHelper.createNodeForChild, which throws "Callbacks must add
            // text or a content description in populateNodeForVirtualViewId()" from inside an
            // accessibility callback - taking the whole application down.
            if (string.IsNullOrEmpty(nodeInfo.Text) && string.IsNullOrEmpty(nodeInfo.ContentDescription))
            {
                nodeInfo.ContentDescription = EmptyNodeDescription;
            }
        }
    }
}
