using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.DBus;
using Avalonia.FreeDesktop.AtSpi.Handlers;
using Avalonia.Logging;
using static Avalonia.FreeDesktop.AtSpi.AtSpiConstants;

namespace Avalonia.FreeDesktop.AtSpi
{
    /// <summary>
    /// Represents an element in the AT-SPI tree, backed by an AutomationPeer.
    /// </summary>
    internal partial class AtSpiNode
    {
        private protected bool _detached;
        private bool _attached;
        private bool _childrenDirty = true;
        private List<AtSpiNode> _attachedChildren = [];

        private readonly string _path;

        protected AtSpiNode(AutomationPeer peer, AtSpiServer server)
        {
            Peer = peer;
            Server = server;
            _path = server.AllocateNodePath();
        }

        public AutomationPeer Peer { get; }
        public AtSpiServer Server { get; }
        public string Path => _path;
        internal bool IsAttached => _attached && !_detached;
        internal AtSpiNode? Parent { get; private set; }
        internal IReadOnlyList<AtSpiNode> AttachedChildren => _attachedChildren;
        internal Task<IDisposable>? PathRegistrationTask { get; private set; }

        public HashSet<string> GetSupportedInterfaces()
        {
            var interfaces = new HashSet<string>(StringComparer.Ordinal) { IfaceAccessible, IfaceComponent };
            if (ApplicationHandler is not null) interfaces.Add(IfaceApplication);
            if (ActionHandler is not null) interfaces.Add(IfaceAction);
            if (ValueHandler is not null) interfaces.Add(IfaceValue);
            if (SelectionHandler is not null) interfaces.Add(IfaceSelection);
            if (TextHandler is not null) interfaces.Add(IfaceText);
            if (EditableTextHandler is not null) interfaces.Add(IfaceEditableText);
            if (ImageHandler is not null) interfaces.Add(IfaceImage);
            return interfaces;
        }

        internal AtSpiAccessibleHandler? AccessibleHandler { get; private set; }
        internal ApplicationNodeApplicationHandler? ApplicationHandler { get; private set; }
        internal AtSpiComponentHandler? ComponentHandler { get; private set; }
        internal AtSpiActionHandler? ActionHandler { get; private set; }
        internal AtSpiValueHandler? ValueHandler { get; private set; }
        internal AtSpiSelectionHandler? SelectionHandler { get; private set; }
        internal AtSpiTextHandler? TextHandler { get; private set; }
        internal AtSpiEditableTextHandler? EditableTextHandler { get; private set; }
        internal AtSpiImageHandler? ImageHandler { get; private set; }
        internal AtSpiEventObjectHandler? EventObjectHandler { get; private set; }
        internal AtSpiEventWindowHandler? EventWindowHandler { get; private set; }

        internal void BuildAndRegisterHandlers(
            IDBusConnection connection,
            SynchronizationContext? synchronizationContext = null)
        {
            var previousRegistrationTask = PathRegistrationTask;

            var targets = new List<object>();

            // Accessible - always present
            targets.Add(AccessibleHandler = new AtSpiAccessibleHandler(Server, this));

            if (Peer.GetProvider<IRootProvider>() is not null)
                targets.Add(ApplicationHandler = new ApplicationNodeApplicationHandler());

            // Component - all visual elements
            targets.Add(ComponentHandler = new AtSpiComponentHandler(Server, this));

            if (Peer.GetProvider<IInvokeProvider>() is not null ||
                Peer.GetProvider<IToggleProvider>() is not null ||
                Peer.GetProvider<IExpandCollapseProvider>() is not null ||
                Peer.GetProvider<IScrollProvider>() is not null ||
                Peer.GetProvider<ISelectionItemProvider>() is not null)
            {
                targets.Add(ActionHandler = new AtSpiActionHandler(Server, this));
            }

            if (Peer.GetProvider<IRangeValueProvider>() is not null)
                targets.Add(ValueHandler = new AtSpiValueHandler(Server, this));

            if (Peer.GetProvider<ISelectionProvider>() is not null)
                targets.Add(SelectionHandler = new AtSpiSelectionHandler(Server, this));

            if (Peer.GetProvider<IValueProvider>() is { } valueProvider
                && Peer.GetProvider<IRangeValueProvider>() is null)
            {
                targets.Add(TextHandler = new AtSpiTextHandler(this));

                if (!valueProvider.IsReadOnly)
                    targets.Add(EditableTextHandler = new AtSpiEditableTextHandler(this));
            }

            if (Peer.GetAutomationControlType() == AutomationControlType.Image)
                targets.Add(ImageHandler = new AtSpiImageHandler(Server, this));

            // Event handlers - always present
            targets.Add(EventObjectHandler = new AtSpiEventObjectHandler(Server, Path));

            if (this is RootAtSpiNode)
                targets.Add(EventWindowHandler = new AtSpiEventWindowHandler(Server, Path));

            PathRegistrationTask = ReplacePathRegistrationAsync(
                previousRegistrationTask,
                connection,
                targets,
                synchronizationContext);
        }

        internal static AtSpiNode Create(AutomationPeer peer, AtSpiServer server)
        {
            return peer.GetProvider<IRootProvider>() is not null
                ? new RootAtSpiNode(peer, server)
                : new AtSpiNode(peer, server);
        }

        internal static string GetAccessibleName(AutomationPeer peer)
        {
            var name = peer.GetName();
            if (!string.IsNullOrWhiteSpace(name))
                return name;

            var visualTypeName = peer.GetClassName();
            return string.IsNullOrWhiteSpace(visualTypeName) ? string.Empty : visualTypeName;
        }

        internal void Attach(AtSpiNode? parent)
        {
            if (_detached)
                return;

            if (_attached)
            {
                Parent = parent;
                return;
            }

            _attached = true;
            _childrenDirty = true;
            Parent = parent;
            Peer.ChildrenChanged += OnPeerChildrenChanged;
            Peer.PropertyChanged += OnPeerPropertyChanged;

            if (Server.A11yConnection is { } connection)
                BuildAndRegisterHandlers(connection, Server.SyncContext);
        }

        internal void SetParent(AtSpiNode? parent) => Parent = parent;

        internal bool RemoveAttachedChild(AtSpiNode child) => _attachedChildren.Remove(child);

        // Present a selection container's realized item containers as its direct
        // AT-SPI children so SelectChild-by-index and parent lookups work.
        private IReadOnlyList<AutomationPeer> GetChildPeers()
        {
            if (Peer.GetProvider<ISelectionProvider>() is null)
                return Peer.GetChildren();

            var items = new List<AutomationPeer>();
            CollectSelectionItemPeers(Peer.GetChildren(), items);
            return items.Count > 0 ? items : Peer.GetChildren();
        }

        private static void CollectSelectionItemPeers(
            IReadOnlyList<AutomationPeer> peers, List<AutomationPeer> result)
        {
            foreach (var peer in peers)
            {
                if (peer.GetProvider<ISelectionItemProvider>() is not null)
                    result.Add(peer);
                else
                    CollectSelectionItemPeers(peer.GetChildren(), result);
            }
        }

        internal IReadOnlyList<AtSpiNode> EnsureChildren()
        {
            if (!IsAttached)
                return Array.Empty<AtSpiNode>();

            if (_childrenDirty)
                UpdateChildren(null);

            return _attachedChildren;
        }

        /// <summary>
        /// Lists the removals and additions that turn one child list into another.
        /// </summary>
        /// <param name="oldChildren">The children before the change.</param>
        /// <param name="newChildren">The children after the change.</param>
        /// <param name="removed">Receives each removed child with its index in <paramref name="oldChildren"/>, highest index first.</param>
        /// <param name="added">Receives each added child with its index in <paramref name="newChildren"/>, lowest index first.</param>
        /// <remarks>
        /// Applying the removals and then the additions in the order given turns the old list into the new one.
        /// A child that changed position is reported as removed and added.
        /// </remarks>
        internal static void DiffChildren<T>(
            IReadOnlyList<T> oldChildren,
            IReadOnlyList<T> newChildren,
            List<(int Index, T Child)> removed,
            List<(int Index, T Child)> added)
            where T : class
        {
            var oldIndices = new Dictionary<T, int>(oldChildren.Count);
            for (var i = 0; i < oldChildren.Count; i++)
                oldIndices[oldChildren[i]] = i;

            // Children that keep their place must stay in the same order in both lists. Keeping the longest
            // increasing run of old indices, taken in new order, reports the fewest moves.
            var common = new List<T>();
            var commonOldIndices = new List<int>();
            foreach (var child in newChildren)
            {
                if (oldIndices.TryGetValue(child, out var oldIndex))
                {
                    common.Add(child);
                    commonOldIndices.Add(oldIndex);
                }
            }

            var kept = new HashSet<T>();
            foreach (var position in LongestIncreasingRun(commonOldIndices))
                kept.Add(common[position]);

            for (var i = oldChildren.Count - 1; i >= 0; i--)
            {
                if (!kept.Contains(oldChildren[i]))
                    removed.Add((i, oldChildren[i]));
            }

            for (var i = 0; i < newChildren.Count; i++)
            {
                if (!kept.Contains(newChildren[i]))
                    added.Add((i, newChildren[i]));
            }
        }

        // Returns the positions of a longest strictly increasing subsequence of the values.
        private static List<int> LongestIncreasingRun(List<int> values)
        {
            // tails[k] is the position of the smallest value that ends an increasing run of length k + 1.
            var tails = new List<int>();
            var previous = new int[values.Count];
            for (var i = 0; i < values.Count; i++)
            {
                int low = 0, high = tails.Count;
                while (low < high)
                {
                    var middle = (low + high) / 2;
                    if (values[tails[middle]] < values[i])
                        low = middle + 1;
                    else
                        high = middle;
                }

                previous[i] = low > 0 ? tails[low - 1] : -1;
                if (low == tails.Count)
                    tails.Add(i);
                else
                    tails[low] = i;
            }

            var run = new List<int>(tails.Count);
            for (var i = tails.Count > 0 ? tails[tails.Count - 1] : -1; i >= 0; i = previous[i])
                run.Add(i);
            return run;
        }

        // Rebuilds the child list. With an event handler, reports each change to clients as AT-SPI specifies.
        private void UpdateChildren(AtSpiEventObjectHandler? eventHandler)
        {
            var oldChildren = _attachedChildren;
            var childPeers = GetChildPeers();
            var newChildren = new List<AtSpiNode>(childPeers.Count);
            foreach (var childPeer in childPeers)
            {
                var childNode = Server.GetOrCreateNode(childPeer);
                if (Server.AttachNode(childNode, this))
                    newChildren.Add(childNode);
            }

            _attachedChildren = newChildren;
            _childrenDirty = false;

            if (oldChildren.Count == 0 && eventHandler is null)
                return;

            var removed = new List<(int Index, AtSpiNode Child)>();
            var added = new List<(int Index, AtSpiNode Child)>();
            DiffChildren(oldChildren, newChildren, removed, added);

            if (removed.Count > 0)
            {
                var remaining = new HashSet<AtSpiNode>(newChildren);
                foreach (var (index, child) in removed)
                {
                    // The reference must be sent before the child is detached, when it becomes the null reference.
                    eventHandler?.EmitChildrenChangedSignal("remove", index, GetChildVariant(child));

                    if (!remaining.Contains(child) && ReferenceEquals(child.Parent, this))
                        Server.DetachSubtreeRecursive(child);
                }
            }

            if (eventHandler is null)
                return;

            foreach (var (index, child) in added)
                eventHandler.EmitChildrenChangedSignal("add", index, GetChildVariant(child));
        }

        private DBusVariant GetChildVariant(AtSpiNode child) =>
            new(Server.GetReference(child).ToDbusStruct());

        public virtual void Detach()
        {
            if (_detached)
                return;

            _detached = true;
            _attached = false;
            _childrenDirty = true;
            _attachedChildren.Clear();
            Parent = null;
            Peer.ChildrenChanged -= OnPeerChildrenChanged;
            Peer.PropertyChanged -= OnPeerPropertyChanged;
            DisposePathRegistration();
        }

        internal async Task DisposePathRegistrationAsync()
        {
            var registrationTask = PathRegistrationTask;
            PathRegistrationTask = null;
            await DisposeRegistrationAsync(registrationTask).ConfigureAwait(false);
        }

        internal void DisposePathRegistration()
        {
            var registrationTask = PathRegistrationTask;
            PathRegistrationTask = null;

            if (registrationTask is null)
                return;

            if (registrationTask.IsCompletedSuccessfully)
            {
                registrationTask.Result.Dispose();
                return;
            }

            _ = DisposeRegistrationAsync(registrationTask);
        }

        private async Task<IDisposable> ReplacePathRegistrationAsync(
            Task<IDisposable>? previousRegistrationTask,
            IDBusConnection connection,
            IReadOnlyCollection<object> targets,
            SynchronizationContext? synchronizationContext)
        {
            await DisposeRegistrationAsync(previousRegistrationTask).ConfigureAwait(false);
            return await connection.RegisterObjects((DBusObjectPath)Path, targets, synchronizationContext)
                .ConfigureAwait(false);
        }

        private static async Task DisposeRegistrationAsync(Task<IDisposable>? registrationTask)
        {
            if (registrationTask is null)
                return;

            try
            {
                var registration = await registrationTask.ConfigureAwait(false);
                registration.Dispose();
            }
            catch (Exception e)
            {
                // Best-effort cleanup: path may have failed to register or connection may be gone.
                Logger.TryGet(LogEventLevel.Debug, LogArea.FreeDesktopPlatform)?
                    .Log(null, "AT-SPI node path registration cleanup failed: {Exception}", e);
            }
        }

        private void OnPeerChildrenChanged(object? sender, EventArgs e)
        {
            if (Server.A11yConnection is null || !IsAttached)
                return;

            // No client has read these children, so there is nothing to report. The next read builds them.
            if (_childrenDirty)
                return;

            UpdateChildren(Server.HasEventListeners ? EventObjectHandler : null);
        }

        private void OnPeerPropertyChanged(object? sender, AutomationPropertyChangedEventArgs e)
        {
            if (Server.A11yConnection is null || !Server.HasEventListeners)
                return;

            if (EventObjectHandler is not { } eventHandler)
                return;

            if (e.Property == AutomationElementIdentifiers.NameProperty)
            {
                eventHandler.EmitPropertyChangeSignal(
                    "accessible-name",
                    new DBusVariant(GetAccessibleName(Peer)));
            }
            else if (e.Property == AutomationElementIdentifiers.HelpTextProperty)
            {
                eventHandler.EmitPropertyChangeSignal(
                    "accessible-description",
                    new DBusVariant(e.NewValue?.ToString() ?? string.Empty));
            }
            else if (e.Property == TogglePatternIdentifiers.ToggleStateProperty)
            {
                var newState = e.NewValue is ToggleState ts ? ts : ToggleState.Off;
                eventHandler.EmitStateChangedSignal(
                    "checked", newState == ToggleState.On ? 1 : 0, new DBusVariant(0));
                eventHandler.EmitStateChangedSignal(
                    "indeterminate", newState == ToggleState.Indeterminate ? 1 : 0, new DBusVariant(0));
            }
            else if (e.Property == ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty)
            {
                var newState = e.NewValue is ExpandCollapseState ecs ? ecs : ExpandCollapseState.Collapsed;
                eventHandler.EmitStateChangedSignal(
                    "expanded", newState == ExpandCollapseState.Expanded ? 1 : 0, new DBusVariant(0));
                eventHandler.EmitStateChangedSignal(
                    "collapsed", newState == ExpandCollapseState.Collapsed ? 1 : 0, new DBusVariant(0));
            }
            else if (e.Property == ValuePatternIdentifiers.ValueProperty)
            {
                eventHandler.EmitPropertyChangeSignal(
                    "accessible-value",
                    new DBusVariant(e.NewValue?.ToString() ?? string.Empty));
            }
            else if (e.Property == SelectionPatternIdentifiers.SelectionProperty)
            {
                eventHandler.EmitSelectionChangedSignal();
            }
            else if (e.Property == AutomationElementIdentifiers.BoundingRectangleProperty)
            {
                eventHandler.EmitBoundsChangedSignal();
            }
        }
    }
}
