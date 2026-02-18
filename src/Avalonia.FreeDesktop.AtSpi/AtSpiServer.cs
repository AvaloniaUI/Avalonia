using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.DBus;
using Avalonia.FreeDesktop.AtSpi.DBusXml;
using Avalonia.FreeDesktop.AtSpi.Handlers;
using Avalonia.Threading;
using static Avalonia.FreeDesktop.AtSpi.AtSpiConstants;

namespace Avalonia.FreeDesktop.AtSpi
{
    internal sealed class AtSpiServer : IAsyncDisposable
    {
        private readonly Dictionary<string, AtSpiNode> _nodesByPath = new(StringComparer.Ordinal);
        private readonly Dictionary<string, ActivePathRegistration> _pathRegistrations = new(StringComparer.Ordinal);

        private DBusConnection? _a11yConnection;
        private string _uniqueName = string.Empty;
        private SynchronizationContext? _syncContext;
        private ApplicationAtSpiNode? _appRoot;
        private AtSpiCacheHandler? _cacheHandler;
        private AtSpiEventObjectHandler? _appRootEventHandler;

        // Registry event listener tracking
        private OrgA11yAtspiRegistryProxy? _registryProxy;
        private IDisposable? _registryRegisteredSubscription;
        private IDisposable? _registryDeregisteredSubscription;
        private IDisposable? _registryOwnerChangedSubscription;
        private string? _registryUniqueName;
        private readonly HashSet<string> _registeredEvents = new(StringComparer.Ordinal);

        internal DBusConnection? A11yConnection => _a11yConnection;
        internal string UniqueName => _uniqueName;

        /// <summary>
        /// Indicates whether any screen reader is currently listening for object events.
        /// Defaults to true (chatty) until registry tracking confirms otherwise.
        /// </summary>
        internal bool HasEventListeners { get; private set; } = true;

        /// <summary>
        /// Starts the AT-SPI server. Must be called on the UI thread.
        /// Creates the synthetic application root and registers with the a11y bus.
        /// </summary>
        public async Task StartAsync()
        {
            // 1. Create a SynchronizationContext that dispatches to the Avalonia UI thread.
            _syncContext = new AvaloniaSynchronizationContext(DispatcherPriority.Normal);

            // 2. Get a11y bus address
            var address = await GetAccessibilityBusAddressAsync();
            
            if (string.IsNullOrWhiteSpace(address))
                throw new InvalidOperationException("Failed to resolve the accessibility bus address.");

            // 3. Connect to a11y bus
            _a11yConnection = await DBusConnection.ConnectAsync(address);
            _uniqueName = await _a11yConnection.GetUniqueNameAsync() ?? string.Empty;

            // 4. Create synthetic application root
            _appRoot = new ApplicationAtSpiNode(null);

            // 5. Build and register handlers for app root
            _cacheHandler = new AtSpiCacheHandler();
            BuildAndRegisterAppRoot();

            // 6. Register cache handler path
            RegisterCachePath();

            // 7. Embed with registry
            await EmbedApplicationAsync();

            // 8. Start tracking registry event listeners
            await InitializeRegistryEventTrackingAsync();
        }

        /// <summary>
        /// Adds a window to the AT-SPI tree. Idempotent — skips if already registered.
        /// </summary>
        public void AddWindow(AutomationPeer windowPeer)
        {
            if (_a11yConnection is null || _appRoot is null)
                return;

            // Idempotent check
            var existing = AtSpiNode.TryGet(windowPeer);
            if (existing is not null && _nodesByPath.ContainsKey(existing.Path))
                return;

            // Create window node
            var windowNode = AtSpiNode.GetOrCreate(windowPeer, this) as RootAtSpiNode;
            if (windowNode is null)
                return;

            windowNode.AppRoot = _appRoot;
            RegisterNodeInternal(windowNode);

            // Add as child of app root
            _appRoot.AddWindowChild(windowNode);

            // Emit children-changed on app root (guarded by event listeners)
            if (HasEventListeners && _appRootEventHandler is { } eventHandler)
            {
                var childRef = GetReference(windowNode);
                var childVariant = new DBusVariant(childRef.ToDbusStruct());
                eventHandler.EmitChildrenChangedSignal(
                    "add", _appRoot.WindowChildren.Count - 1, childVariant);
            }
        }

        /// <summary>
        /// Removes a window from the AT-SPI tree.
        /// </summary>
        public void RemoveWindow(AutomationPeer windowPeer)
        {
            if (_a11yConnection is null || _appRoot is null)
                return;

            var windowNode = AtSpiNode.TryGet(windowPeer) as RootAtSpiNode;
            if (windowNode is null)
                return;

            // Emit children-changed("remove") on app root before removal (guarded by event listeners)
            if (HasEventListeners && _appRootEventHandler is { } eventHandler)
            {
                var index = _appRoot.WindowChildren.IndexOf(windowNode);
                var childRef = GetReference(windowNode);
                var childVariant = new DBusVariant(childRef.ToDbusStruct());
                eventHandler.EmitChildrenChangedSignal("remove", index, childVariant);
            }

            // Collect all descendant nodes under this window
            var pathPrefix = windowNode.Path;
            var toRemove = _nodesByPath
                .Where(kv => kv.Key == pathPrefix || kv.Key.StartsWith(pathPrefix + "/", StringComparison.Ordinal))
                .Select(kv => kv.Value)
                .ToList();

            // Also include nodes that belong to this window's subtree
            // Walk through all registered nodes and find those whose peer chain leads back to windowPeer
            var allToRemove = new List<AtSpiNode>(toRemove);
            foreach (var kv in _nodesByPath.ToArray())
            {
                if (kv.Value is { } node && node != windowNode && !allToRemove.Contains(node))
                {
                    // Check if this node's visual root is the window
                    try
                    {
                        var visualRoot = node.Peer.GetVisualRoot();
                        if (visualRoot is not null && ReferenceEquals(AtSpiNode.TryGet(visualRoot), windowNode))
                            allToRemove.Add(node);
                    }
                    catch
                    {
                        // Peer may be in a defunct state
                    }
                }
            }

            RemoveNodes(allToRemove, emitDefunct: true);
        }

        /// <summary>
        /// Ensures a node is registered in the tree (idempotent).
        /// Called after <see cref="AtSpiNode.GetOrCreate"/> to lazily register new nodes.
        /// Also detects when a node's supported interfaces have changed (e.g. a peer
        /// gained IValueProvider after content was loaded) and re-registers the D-Bus
        /// path with updated handlers.
        /// </summary>
        internal void EnsureNodeRegistered(AtSpiNode node)
        {
            if (_nodesByPath.ContainsKey(node.Path))
            {
                RebuildHandlersIfStale(node);
                return;
            }

            RegisterNodeInternal(node);
        }

        /// <summary>
        /// Checks whether a node's supported interfaces have changed since its handlers
        /// were built. If so, tears down the old D-Bus registration, rebuilds handlers,
        /// and re-registers. This handles the case where a peer gains or loses providers
        /// after initial registration (e.g. TextBox content loading into a container).
        /// </summary>
        /// <returns>True if the registration was updated.</returns>
        private bool RebuildHandlersIfStale(AtSpiNode node)
        {
            if (node.Handlers?.RegisteredInterfaces is not { } oldInterfaces)
                return false;

            var currentInterfaces = node.GetSupportedInterfaces();
            if (currentInterfaces.SetEquals(oldInterfaces))
                return false;

            // Interfaces changed — tear down and rebuild
            UnregisterNodePath(node.Path);
            node.Handlers = null;
            BuildHandlersForNode(node);
            RegisterNodePath(node);
            return true;
        }

        public async ValueTask DisposeAsync()
        {
            _registryOwnerChangedSubscription?.Dispose();
            _registryOwnerChangedSubscription = null;
            _registryRegisteredSubscription?.Dispose();
            _registryRegisteredSubscription = null;
            _registryDeregisteredSubscription?.Dispose();
            _registryDeregisteredSubscription = null;
            _registryProxy = null;
            _registryUniqueName = null;
            _registeredEvents.Clear();

            foreach (var registration in _pathRegistrations.Values)
                registration.Registration.Dispose();
            _pathRegistrations.Clear();

            if (_a11yConnection is not null)
            {
                await _a11yConnection.DisposeAsync();
                _a11yConnection = null;
            }

            foreach (var node in _nodesByPath.Values.ToArray())
                ReleaseNode(node);

            _uniqueName = string.Empty;
            _cacheHandler = null;
            _appRoot = null;
            _appRootEventHandler = null;
            _nodesByPath.Clear();
        }

        internal AtSpiObjectReference GetReference(AtSpiNode? node)
        {
            return node is null ? GetNullReference() : 
                new AtSpiObjectReference(_uniqueName, new DBusObjectPath(node.Path));
        }

        internal AtSpiObjectReference GetNullReference()
        {
            return new AtSpiObjectReference(string.Empty, new DBusObjectPath(NullPath));
        }

        internal AtSpiObjectReference GetRootReference()
        {
            return new AtSpiObjectReference(_uniqueName, new DBusObjectPath(RootPath));
        }

        internal void EmitChildrenChanged(AtSpiNode node)
        {
            if (_a11yConnection is null)
                return;

            // Ensure child nodes are registered on D-Bus
            var childPeers = node.Peer.GetChildren();
            var liveChildren = new HashSet<AutomationPeer>(AutomationPeerReferenceComparer.Instance);
            foreach (var t in childPeers)
            {
                var childNode = AtSpiNode.GetOrCreate(t, this);
                EnsureNodeRegistered(childNode);
                liveChildren.Add(t);
            }

            PruneRemovedChildren(node, liveChildren);
            PruneDisconnectedNodes();

            // Emit children-changed event (guarded by event listeners)
            if (HasEventListeners && node.Handlers?.EventObjectHandler is { } eventHandler)
            {
                var reference = GetReference(node);
                var childVariant = new DBusVariant(reference.ToDbusStruct());
                eventHandler.EmitChildrenChangedSignal("add", 0, childVariant);
            }
        }

        internal void EmitPropertyChange(AtSpiNode node, AutomationPropertyChangedEventArgs e)
        {
            if (_a11yConnection is null || !HasEventListeners)
                return;

            if (node.Handlers?.EventObjectHandler is not { } eventHandler)
                return;

            if (e.Property == AutomationElementIdentifiers.NameProperty)
            {
                eventHandler.EmitPropertyChangeSignal(
                    "accessible-name",
                    new DBusVariant(e.NewValue?.ToString() ?? string.Empty));
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

        internal void EmitWindowActivationChange(RootAtSpiNode windowNode, bool active)
        {
            if (_a11yConnection is null || !HasEventListeners)
                return;

            if (windowNode.Handlers?.EventObjectHandler is { } eventHandler)
                eventHandler.EmitStateChangedSignal("active", active ? 1 : 0, new DBusVariant(0));

            if (windowNode.Handlers?.EventWindowHandler is { } windowHandler)
            {
                if (active)
                    windowHandler.EmitActivateSignal();
                else
                    windowHandler.EmitDeactivateSignal();
            }
        }

        internal void EmitFocusChange(AtSpiNode? focusedNode)
        {
            if (_a11yConnection is null || focusedNode is null || !HasEventListeners)
                return;

            if (focusedNode.Handlers?.EventObjectHandler is { } eventHandler)
            {
                eventHandler.EmitStateChangedSignal("focused", 1, new DBusVariant(0));
            }
        }

        // ── Registry event listener tracking ──────────────────────────────

        private async Task InitializeRegistryEventTrackingAsync()
        {
            if (_a11yConnection is null)
                return;

            try
            {
                _registryProxy ??= new OrgA11yAtspiRegistryProxy(
                    _a11yConnection, BusNameRegistry, new DBusObjectPath(RegistryPath));

                // Seed from current registrations
                var events = await _registryProxy.GetRegisteredEventsAsync();
                _registeredEvents.Clear();
                foreach (var registered in events)
                    _registeredEvents.Add(registered.EventName);
                UpdateHasEventListeners();

                // Resolve registry unique name and subscribe to signals
                var registryOwner = await _a11yConnection.GetNameOwnerAsync(BusNameRegistry);
                await SubscribeToRegistrySignalsAsync(registryOwner);

                // Watch for registry daemon restarts
                _registryOwnerChangedSubscription ??= await _a11yConnection.WatchNameOwnerChangedAsync(
                    (name, oldOwner, newOwner) =>
                    {
                        if (!string.Equals(name, BusNameRegistry, StringComparison.Ordinal))
                            return;

                        _ = SubscribeToRegistrySignalsAsync(newOwner);
                    },
                    emitOnCapturedContext: true);
            }
            catch
            {
                // Registry event tracking unavailable — remain chatty.
                HasEventListeners = true;
            }
        }

        private async Task SubscribeToRegistrySignalsAsync(string? registryOwner)
        {
            if (_a11yConnection is null)
                return;

            if (string.Equals(_registryUniqueName, registryOwner, StringComparison.Ordinal))
                return;

            // Dispose old subscriptions
            _registryRegisteredSubscription?.Dispose();
            _registryRegisteredSubscription = null;
            _registryDeregisteredSubscription?.Dispose();
            _registryDeregisteredSubscription = null;
            _registryUniqueName = registryOwner;

            var senderFilter = string.IsNullOrWhiteSpace(registryOwner) ? null : registryOwner;

            _registryProxy ??= new OrgA11yAtspiRegistryProxy(
                _a11yConnection, BusNameRegistry, new DBusObjectPath(RegistryPath));

            try
            {
                _registryRegisteredSubscription = await _registryProxy.WatchEventListenerRegisteredAsync(
                    OnRegistryEventListenerRegistered,
                    senderFilter,
                    emitOnCapturedContext: true);

                _registryDeregisteredSubscription = await _registryProxy.WatchEventListenerDeregisteredAsync(
                    OnRegistryEventListenerDeregistered,
                    senderFilter,
                    emitOnCapturedContext: true);
            }
            catch
            {
                _registryRegisteredSubscription?.Dispose();
                _registryRegisteredSubscription = null;
                _registryDeregisteredSubscription?.Dispose();
                _registryDeregisteredSubscription = null;
                HasEventListeners = true;
            }
        }

        private void OnRegistryEventListenerRegistered(string bus, string @event, List<string> properties)
        {
            _registeredEvents.Add(@event);
            UpdateHasEventListeners();
        }

        private void OnRegistryEventListenerDeregistered(string bus, string @event)
        {
            _registeredEvents.Remove(@event);
            UpdateHasEventListeners();
        }

        private void UpdateHasEventListeners()
        {
            HasEventListeners = _registeredEvents.Any(IsObjectEventClass);
        }

        private static bool IsObjectEventClass(string eventName)
        {
            if (string.IsNullOrWhiteSpace(eventName))
                return false;

            if (eventName == "*")
                return true;

            return eventName.StartsWith("object:", StringComparison.OrdinalIgnoreCase)
                || eventName.StartsWith("window:", StringComparison.OrdinalIgnoreCase)
                || eventName.StartsWith("focus:", StringComparison.OrdinalIgnoreCase);
        }

        // ── Node registration ────────────────────────────────────────────

        /// <summary>
        /// Registers a node in _nodesByPath, builds handlers, and registers D-Bus path.
        /// </summary>
        private void RegisterNodeInternal(AtSpiNode node)
        {
            _nodesByPath[node.Path] = node;
            BuildHandlersForNode(node);
            RegisterNodePath(node);
        }

        private void BuildAndRegisterAppRoot()
        {
            if (_a11yConnection is null || _appRoot is null)
                return;

            var accessibleHandler = new ApplicationAccessibleHandler(this, _appRoot);
            var applicationHandler = new ApplicationNodeApplicationHandler();
            var eventHandler = new AtSpiEventObjectHandler(this, _appRoot.Path);
            _appRootEventHandler = eventHandler;

            var targets = new List<object> { accessibleHandler, applicationHandler, eventHandler };
            var registration = _a11yConnection.RegisterObjects(
                (DBusObjectPath)RootPath, targets, _syncContext);
            _pathRegistrations[RootPath] = new ActivePathRegistration(accessibleHandler, registration);
        }

        private void BuildHandlersForNode(AtSpiNode node)
        {
            if (node.Handlers is not null)
                return;

            var handlers = new AtSpiNodeHandlers(node);
            var interfaces = node.GetSupportedInterfaces();
            handlers.RegisteredInterfaces = interfaces;

            if (interfaces.Contains(IfaceAccessible))
                handlers.AccessibleHandler = new AtSpiAccessibleHandler(this, node);

            if (interfaces.Contains(IfaceApplication))
                handlers.ApplicationHandler = new AtSpiApplicationHandler(this, node);

            if (interfaces.Contains(IfaceComponent))
                handlers.ComponentHandler = new AtSpiComponentHandler(this, node);

            if (interfaces.Contains(IfaceAction))
                handlers.ActionHandler = new AtSpiActionHandler(this, node);

            if (interfaces.Contains(IfaceValue))
                handlers.ValueHandler = new AtSpiValueHandler(this, node);

            if (interfaces.Contains(IfaceSelection))
                handlers.SelectionHandler = new AtSpiSelectionHandler(this, node);

            if (interfaces.Contains(IfaceText))
                handlers.TextHandler = new AtSpiTextHandler(this, node);

            if (interfaces.Contains(IfaceEditableText))
                handlers.EditableTextHandler = new AtSpiEditableTextHandler(this, node);

            if (interfaces.Contains(IfaceImage))
                handlers.ImageHandler = new AtSpiImageHandler(this, node);

            // if (interfaces.Contains(IfaceCollection))
            //     handlers.CollectionHandler = new AtSpiCollectionHandler(this, node);

            handlers.EventObjectHandler = new AtSpiEventObjectHandler(this, node.Path);

            if (node is RootAtSpiNode)
                handlers.EventWindowHandler = new AtSpiEventWindowHandler(this, node.Path);

            node.Handlers = handlers;
        }

        private void RegisterNodePath(AtSpiNode node)
        {
            if (_a11yConnection is null || node.Handlers is null)
                return;

            if (_pathRegistrations.ContainsKey(node.Path))
                return;

            var registration = node.Handlers.Register(_a11yConnection, _syncContext);
            _pathRegistrations[node.Path] = new ActivePathRegistration(node.Handlers, registration);
        }

        private void UnregisterNodePath(string path)
        {
            if (!_pathRegistrations.TryGetValue(path, out var active)) return;
            active.Registration.Dispose();
            _pathRegistrations.Remove(path);
        }

        private void RegisterCachePath()
        {
            if (_a11yConnection is null || _cacheHandler is null)
                return;

            var registration = _a11yConnection.RegisterObjects(
                (DBusObjectPath)CachePath, [_cacheHandler], _syncContext);
            _pathRegistrations[CachePath] = new ActivePathRegistration(_cacheHandler, registration);
        }

        private async Task<string> GetAccessibilityBusAddressAsync()
        {
            try
            {
                await using var connection = await DBusConnection.ConnectSessionAsync();
                var proxy = new OrgA11yBusProxy(connection, BusNameA11y, new DBusObjectPath(PathA11y));
                return await proxy.GetAddressAsync();
            }
            catch
            {
                return string.Empty;
            }
        }

        private async Task EmbedApplicationAsync()
        {
            if (_a11yConnection is null)
                return;

            var proxy = new OrgA11yAtspiSocketProxy(_a11yConnection, BusNameRegistry, new DBusObjectPath(RootPath));
            await proxy.EmbedAsync(new AtSpiObjectReference(_uniqueName, new DBusObjectPath(RootPath)));
        }

        private void PruneRemovedChildren(AtSpiNode parentNode, HashSet<AutomationPeer> liveChildren)
        {
            var removedChildRoots = _nodesByPath.Values
                .Where(node => !ReferenceEquals(node, parentNode)
                               && IsDirectChild(node.Peer, parentNode.Peer)
                               && !liveChildren.Contains(node.Peer))
                .ToList();

            foreach (var removedRoot in removedChildRoots)
                RemoveSubtree(removedRoot.Peer);
        }

        private void RemoveSubtree(AutomationPeer rootPeer)
        {
            var subtreeNodes = _nodesByPath.Values
                .Where(node => IsDescendantOrSelf(node.Peer, rootPeer))
                .ToList();

            RemoveNodes(subtreeNodes, emitDefunct: true);
        }

        private void RemoveNodes(IEnumerable<AtSpiNode> nodes, bool emitDefunct)
        {
            foreach (var node in nodes.Distinct().ToList())
            {
                if (!_nodesByPath.ContainsKey(node.Path))
                    continue;

                if (emitDefunct && HasEventListeners && node.Handlers?.EventObjectHandler is { } eventHandler)
                    eventHandler.EmitStateChangedSignal("defunct", 1, new DBusVariant("0"));

                UnregisterNodePath(node.Path);
                _nodesByPath.Remove(node.Path);

                if (node is RootAtSpiNode rootNode)
                    _appRoot?.RemoveWindowChild(rootNode);

                ReleaseNode(node);
            }
        }

        private void PruneDisconnectedNodes()
        {
            if (_appRoot is null || _appRoot.WindowChildren.Count == 0 || _nodesByPath.Count == 0)
                return;

            var livePeers = new HashSet<AutomationPeer>(AutomationPeerReferenceComparer.Instance);
            var toVisit = new Stack<AutomationPeer>();
            foreach (var window in _appRoot.WindowChildren)
                toVisit.Push(window.Peer);

            while (toVisit.Count > 0)
            {
                var peer = toVisit.Pop();
                if (!livePeers.Add(peer))
                    continue;

                try
                {
                    foreach (var child in peer.GetChildren())
                    {
                        toVisit.Push(child);
                    }
                }
                catch
                {
                    // Defunct peers may throw while querying children.
                }
            }

            var staleNodes = _nodesByPath.Values
                .Where(node => !livePeers.Contains(node.Peer))
                .ToList();

            if (staleNodes.Count > 0)
                RemoveNodes(staleNodes, emitDefunct: true);
        }

        private static bool IsDirectChild(AutomationPeer node, AutomationPeer parent)
        {
            try
            {
                return ReferenceEquals(node.GetParent(), parent);
            }
            catch
            {
                return false;
            }
        }

        private static bool IsDescendantOrSelf(AutomationPeer node, AutomationPeer ancestor)
        {
            try
            {
                for (var current = node; current is not null; current = current.GetParent())
                {
                    if (ReferenceEquals(current, ancestor))
                        return true;
                }
            }
            catch
            {
                // A defunct peer may throw while walking its parent chain.
            }

            return false;
        }

        private static void ReleaseNode(AtSpiNode node)
        {
            node.Detach();
            AtSpiNode.Release(node.Peer);
            node.Handlers = null;
        }

        private sealed class ActivePathRegistration(object owner, IDisposable registration)
        {
            public object Owner { get; } = owner;
            public IDisposable Registration { get; } = registration;
        }

        private sealed class AutomationPeerReferenceComparer : IEqualityComparer<AutomationPeer>
        {
            public static AutomationPeerReferenceComparer Instance { get; } = new();

            public bool Equals(AutomationPeer? x, AutomationPeer? y) => ReferenceEquals(x, y);

            public int GetHashCode(AutomationPeer obj) => RuntimeHelpers.GetHashCode(obj);
        }
    }
}
