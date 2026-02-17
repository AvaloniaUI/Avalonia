using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
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
            // We cannot rely on SynchronizationContext.Current here because StartAsync
            // may be called from an async continuation on a thread-pool thread (the
            // fire-and-forget InitAccessibilityAsync path yields after the first real
            // await, and the continuation has no ambient context).
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
            _cacheHandler = new AtSpiCacheHandler(this);
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
            if (_a11yConnection is null || _appRoot is null || _cacheHandler is null)
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

            // Emit cache add for the window node (always — cache signals are never suppressed)
            _cacheHandler.EmitAddAccessibleSignal(BuildCacheItem(windowNode));

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
            if (_a11yConnection is null || _appRoot is null || _cacheHandler is null)
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
                if (kv.Value is AtSpiNode node && node != windowNode && !allToRemove.Contains(node))
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

            // Emit cache removes and unregister paths
            foreach (var node in allToRemove)
            {
                _cacheHandler.EmitRemoveAccessibleSignal(GetReference(node));
                UnregisterNodePath(node.Path);
                _nodesByPath.Remove(node.Path);
            }

            _appRoot.RemoveWindowChild(windowNode);
        }

        /// <summary>
        /// Ensures a node is registered in the tree (idempotent).
        /// Called after <see cref="AtSpiNode.GetOrCreate"/> to lazily register new nodes.
        /// </summary>
        internal void EnsureNodeRegistered(AtSpiNode node)
        {
            if (_nodesByPath.ContainsKey(node.Path))
                return;

            RegisterNodeInternal(node);

            _cacheHandler?.EmitAddAccessibleSignal(BuildCacheItem(node));
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

            _uniqueName = string.Empty;
            _cacheHandler = null;
            _appRoot = null;
            _appRootEventHandler = null;
            _nodesByPath.Clear();
        }

        internal IReadOnlyCollection<AtSpiNode> GetAllNodes()
        {
            return _nodesByPath.Values.ToArray();
        }

        internal AtSpiObjectReference GetReference(AtSpiNode? node)
        {
            if (node is null)
                return GetNullReference();
            return new AtSpiObjectReference(_uniqueName, new DBusObjectPath(node.Path));
        }

        internal AtSpiObjectReference GetNullReference()
        {
            return new AtSpiObjectReference(string.Empty, new DBusObjectPath(NullPath));
        }

        internal AtSpiObjectReference GetRootReference()
        {
            return new AtSpiObjectReference(_uniqueName, new DBusObjectPath(RootPath));
        }

        internal AtSpiAccessibleCacheItem BuildCacheItem(AtSpiNode node)
        {
            var self = new AtSpiObjectReference(_uniqueName, new DBusObjectPath(node.Path));
            var app = new AtSpiObjectReference(_uniqueName, new DBusObjectPath(RootPath));

            AtSpiObjectReference parent;
            int indexInParent;

            if (node is RootAtSpiNode { AppRoot: { } appRoot })
            {
                // Window node: parent is app root
                parent = new AtSpiObjectReference(_uniqueName, new DBusObjectPath(appRoot.Path));
                indexInParent = appRoot.WindowChildren.IndexOf((RootAtSpiNode)node);
            }
            else
            {
                var parentPeer = node.Peer.GetParent();
                var parentNode = AtSpiNode.TryGet(parentPeer);
                parent = parentNode is not null
                    ? new AtSpiObjectReference(_uniqueName, new DBusObjectPath(parentNode.Path))
                    : new AtSpiObjectReference(string.Empty, new DBusObjectPath(NullPath));

                indexInParent = -1;
                if (parentPeer is not null)
                {
                    var children = parentPeer.GetChildren();
                    for (var i = 0; i < children.Count; i++)
                    {
                        if (ReferenceEquals(children[i], node.Peer))
                        {
                            indexInParent = i;
                            break;
                        }
                    }
                }
            }

            var childCount = node.Peer.GetChildren().Count;
            var interfaces = node.GetSupportedInterfaces()
                .OrderBy(static i => i, StringComparer.Ordinal)
                .ToList();
            var name = node.Peer.GetName();
            var role = (uint)AtSpiNode.ToAtSpiRole(node.Peer.GetAutomationControlType());
            var description = node.Peer.GetHelpText();
            var states = node.ComputeStates();

            return new AtSpiAccessibleCacheItem(
                self,
                app,
                parent,
                indexInParent,
                childCount,
                interfaces,
                name,
                role,
                description,
                states);
        }

        /// <summary>
        /// Builds a cache item for the synthetic application root.
        /// </summary>
        internal AtSpiAccessibleCacheItem BuildAppRootCacheItem()
        {
            if (_appRoot is null)
                throw new InvalidOperationException("App root not initialized.");

            var self = new AtSpiObjectReference(_uniqueName, new DBusObjectPath(RootPath));
            var app = self;
            var parent = new AtSpiObjectReference(string.Empty, new DBusObjectPath(NullPath));
            var childCount = _appRoot.WindowChildren.Count;
            var interfaces = new List<string> { IfaceAccessible, IfaceApplication };
            interfaces.Sort(StringComparer.Ordinal);
            var states = BuildStateSet(new[] { AtSpiState.Active });

            return new AtSpiAccessibleCacheItem(
                self,
                app,
                parent,
                -1,
                childCount,
                interfaces,
                _appRoot.Name,
                (uint)AtSpiRole.Application,
                string.Empty,
                states);
        }

        internal void EmitChildrenChanged(AtSpiNode node)
        {
            if (_a11yConnection is null || _cacheHandler is null)
                return;

            // Emit cache updates for children
            var childPeers = node.Peer.GetChildren();
            for (var i = 0; i < childPeers.Count; i++)
            {
                var childNode = AtSpiNode.GetOrCreate(childPeers[i], this);
                if (childNode is not null)
                {
                    EnsureNodeRegistered(childNode);
                    _cacheHandler.EmitAddAccessibleSignal(BuildCacheItem(childNode));
                }
            }

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

            handlers.EventObjectHandler = new AtSpiEventObjectHandler(this, node.Path);

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
            if (_pathRegistrations.TryGetValue(path, out var active))
            {
                active.Registration.Dispose();
                _pathRegistrations.Remove(path);
            }
        }

        private void RegisterCachePath()
        {
            if (_a11yConnection is null || _cacheHandler is null)
                return;

            var registration = _a11yConnection.RegisterObjects(
                (DBusObjectPath)CachePath, new object[] { _cacheHandler }, _syncContext);
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

        private sealed class ActivePathRegistration
        {
            public ActivePathRegistration(object owner, IDisposable registration)
            {
                Owner = owner;
                Registration = registration;
            }

            public object Owner { get; }
            public IDisposable Registration { get; }
        }
    }
}
