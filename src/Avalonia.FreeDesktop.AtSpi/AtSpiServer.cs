using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.DBus;
using Avalonia.FreeDesktop.AtSpi.DBusXml;
using Avalonia.FreeDesktop.AtSpi.Handlers;
using static Avalonia.FreeDesktop.AtSpi.AtSpiConstants;

namespace Avalonia.FreeDesktop.AtSpi
{
    internal sealed class AtSpiServer : IAsyncDisposable
    {
        private readonly object _treeGate = new();
        private readonly Dictionary<string, AtSpiNode> _nodesByPath = new(StringComparer.Ordinal);
        private readonly Dictionary<string, ActivePathRegistration> _pathRegistrations = new(StringComparer.Ordinal);

        private DBusConnection? _a11yConnection;
        private string _uniqueName = string.Empty;
        private AtSpiCacheHandler? _cacheHandler;
        private RootAtSpiNode? _rootNode;

        internal DBusConnection? A11yConnection => _a11yConnection;
        internal object TreeGate => _treeGate;

        public async Task StartAsync(AutomationPeer rootPeer)
        {
            // 1. Get a11y bus address
            var address = await GetAccessibilityBusAddressAsync();
            if (string.IsNullOrWhiteSpace(address))
                throw new InvalidOperationException("Failed to resolve the accessibility bus address.");

            // 2. Connect to a11y bus
            _a11yConnection = await DBusConnection.ConnectAsync(address);
            _uniqueName = await _a11yConnection.GetUniqueNameAsync() ?? string.Empty;

            // 3. Create root node
            _rootNode = new RootAtSpiNode(rootPeer, this);
            RegisterNode(_rootNode);

            // 4. Walk peer tree
            WalkAndCreateNodes(rootPeer);

            // 5. Build handlers for all nodes
            BuildHandlers();

            // 6. Register D-Bus object paths
            RefreshPathRegistrations();

            // 7. Embed with registry
            await EmbedApplicationAsync();

            // 8. Emit initial cache snapshot
            EmitInitialCacheSnapshot();
        }

        public async ValueTask DisposeAsync()
        {
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
            _rootNode = null;
            _nodesByPath.Clear();
        }

        internal void RegisterNode(AtSpiNode node)
        {
            lock (_treeGate)
            {
                _nodesByPath[node.Path] = node;
            }
        }

        internal void UnregisterNode(AtSpiNode node)
        {
            lock (_treeGate)
            {
                _nodesByPath.Remove(node.Path);
            }
        }

        internal IReadOnlyCollection<AtSpiNode> GetAllNodes()
        {
            lock (_treeGate)
            {
                return _nodesByPath.Values.ToArray();
            }
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

            var parentPeer = node.InvokeSync(() => node.Peer.GetParent());
            var parentNode = AtSpiNode.TryGet(parentPeer);
            var parent = parentNode is not null
                ? new AtSpiObjectReference(_uniqueName, new DBusObjectPath(parentNode.Path))
                : new AtSpiObjectReference(string.Empty, new DBusObjectPath(NullPath));

            var indexInParent = node.InvokeSync(() =>
            {
                var p = node.Peer.GetParent();
                if (p is null) return -1;
                var children = p.GetChildren();
                for (var i = 0; i < children.Count; i++)
                {
                    if (ReferenceEquals(children[i], node.Peer))
                        return i;
                }
                return -1;
            });

            var childCount = node.InvokeSync(() => node.Peer.GetChildren().Count);
            var interfaces = node.InvokeSync(() => node.GetSupportedInterfaces())
                .OrderBy(static i => i, StringComparer.Ordinal)
                .ToList();
            var name = node.InvokeSync(() => node.Peer.GetName());
            var role = (uint)node.InvokeSync(() => AtSpiNode.ToAtSpiRole(node.Peer.GetAutomationControlType()));
            var description = node.InvokeSync(() => node.Peer.GetHelpText());
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

        internal void EmitChildrenChanged(AtSpiNode node)
        {
            if (_a11yConnection is null || _cacheHandler is null)
                return;

            // Re-walk the changed subtree to pick up new nodes
            var peer = node.Peer;
            WalkAndCreateNodes(peer);
            BuildHandlers();
            RefreshPathRegistrations();

            // Emit cache updates
            var childPeers = node.InvokeSync(() => peer.GetChildren());
            for (var i = 0; i < childPeers.Count; i++)
            {
                var childNode = AtSpiNode.GetOrCreate(childPeers[i], this);
                if (childNode is not null)
                    _cacheHandler.EmitAddAccessibleSignal(BuildCacheItem(childNode));
            }

            // Emit children-changed event
            if (node.Handlers?.EventObjectHandler is { } eventHandler)
            {
                var reference = GetReference(node);
                var childVariant = new DBusVariant(reference.ToDbusStruct());
                eventHandler.EmitChildrenChangedSignal("add", 0, childVariant);
            }
        }

        internal void EmitPropertyChange(AtSpiNode node, AutomationPropertyChangedEventArgs e)
        {
            if (_a11yConnection is null)
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
            if (_a11yConnection is null || focusedNode is null)
                return;

            if (focusedNode.Handlers?.EventObjectHandler is { } eventHandler)
            {
                eventHandler.EmitStateChangedSignal("focused", 1, new DBusVariant(0));
            }
        }

        private void WalkAndCreateNodes(AutomationPeer peer)
        {
            var node = AtSpiNode.GetOrCreate(peer, this);
            if (node is not null)
                RegisterNode(node);

            var children = node?.InvokeSync(() => peer.GetChildren());
            if (children is null)
                return;

            foreach (var child in children)
            {
                WalkAndCreateNodes(child);
            }
        }

        private void BuildHandlers()
        {
            AtSpiNode[] snapshot;
            lock (_treeGate)
            {
                snapshot = _nodesByPath.Values.ToArray();
            }

            foreach (var node in snapshot)
            {
                if (node.Handlers is not null)
                    continue;

                var handlers = new AtSpiNodeHandlers(node);
                var interfaces = node.InvokeSync(() => node.GetSupportedInterfaces());

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

            _cacheHandler ??= new AtSpiCacheHandler(this);
        }

        private void RefreshPathRegistrations()
        {
            if (_a11yConnection is null)
                return;

            var desiredRegistrations = new Dictionary<string, (object Owner, Func<IDisposable> Register)>(StringComparer.Ordinal);

            AtSpiNode[] snapshot;
            lock (_treeGate)
            {
                snapshot = _nodesByPath.Values
                    .OrderBy(static n => n.Path, StringComparer.Ordinal)
                    .ToArray();
            }

            foreach (var node in snapshot)
            {
                if (node.Handlers is null)
                    continue;

                var handlers = node.Handlers;
                desiredRegistrations.Add(
                    node.Path,
                    (handlers, () => handlers.Register(_a11yConnection)));
            }

            if (_cacheHandler is not null)
            {
                var cacheHandler = _cacheHandler;
                desiredRegistrations.Add(
                    CachePath,
                    (cacheHandler, () => _a11yConnection.RegisterObjects((DBusObjectPath)CachePath, new object[] { cacheHandler })));
            }

            // Remove stale registrations
            foreach (var (path, active) in _pathRegistrations.ToArray())
            {
                if (!desiredRegistrations.TryGetValue(path, out var desired)
                    || !ReferenceEquals(active.Owner, desired.Owner))
                {
                    active.Registration.Dispose();
                    _pathRegistrations.Remove(path);
                }
            }

            // Add new registrations
            foreach (var (path, desired) in desiredRegistrations)
            {
                if (_pathRegistrations.ContainsKey(path))
                    continue;

                var registration = desired.Register();
                _pathRegistrations.Add(path, new ActivePathRegistration(desired.Owner, registration));
            }
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

        private void EmitInitialCacheSnapshot()
        {
            if (_cacheHandler is null)
                return;

            AtSpiNode[] snapshot;
            lock (_treeGate)
            {
                snapshot = _nodesByPath.Values
                    .OrderBy(static node => node.Path, StringComparer.Ordinal)
                    .ToArray();
            }

            foreach (var node in snapshot)
            {
                _cacheHandler.EmitAddAccessibleSignal(BuildCacheItem(node));
            }
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
