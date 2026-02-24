using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Automation.Peers;
using Avalonia.DBus;
using Avalonia.FreeDesktop.AtSpi.DBusXml;
using Avalonia.FreeDesktop.AtSpi.Handlers;
using Avalonia.Logging;
using Avalonia.Threading;
using static Avalonia.FreeDesktop.AtSpi.AtSpiConstants;

namespace Avalonia.FreeDesktop.AtSpi
{
    /// <summary>
    /// Manages the AT-SPI D-Bus connection, node registration, and event emission.
    /// </summary>
    internal sealed class AtSpiServer : IAsyncDisposable
    {
        private readonly Dictionary<string, AtSpiNode> _nodesByPath = new(StringComparer.Ordinal);
        private readonly Dictionary<AutomationPeer, AtSpiNode> _nodesByPeer = [];
        private readonly object _embedSync = new();

        private int _nextNodeId;
        private DBusConnection? _a11yConnection;
        private string _uniqueName = string.Empty;
        private SynchronizationContext? _syncContext;
        private ApplicationAtSpiNode? _appRoot;
        private AtSpiCacheHandler? _cacheHandler;
        private AtSpiEventObjectHandler? _appRootEventHandler;
        private AtSpiRegistryEventTracker? _registryTracker;
        private IDisposable? _appRootRegistration;
        private IDisposable? _cacheRegistration;
        private Task? _embedTask;
        private bool _isEmbedded;

        internal DBusConnection? A11yConnection => _a11yConnection;
        internal SynchronizationContext? SyncContext => _syncContext;
        internal string UniqueName => _uniqueName;

        /// <summary>
        /// Indicates whether any screen reader is currently listening for object events.
        /// Defaults to true (chatty) until registry tracking confirms otherwise.
        /// </summary>
        internal bool HasEventListeners => _registryTracker?.HasEventListeners ?? true;

        /// <summary>
        /// Starts the AT-SPI server.
        /// Must be called on the UI thread.
        /// <remarks>
        /// Call order in this method is important because
        /// AT's are sensitive and/or broken.
        /// </remarks>
        /// </summary>
        public async Task StartAsync()
        {
            lock (_embedSync)
            {
                _embedTask = null;
                _isEmbedded = false;
            }

            _syncContext = new AvaloniaSynchronizationContext(DispatcherPriority.Normal);

            var address = await GetAccessibilityBusAddressAsync();

            if (string.IsNullOrWhiteSpace(address))
                throw new InvalidOperationException("Failed to resolve the accessibility bus address.");

            _a11yConnection = await DBusConnection.ConnectAsync(address);
            _uniqueName = await _a11yConnection.GetUniqueNameAsync() ?? string.Empty;

            _appRoot = new ApplicationAtSpiNode(null);

            _cacheHandler = new AtSpiCacheHandler();
            await BuildAndRegisterAppRootAsync();

            await RegisterCachePathAsync();

            _registryTracker = new AtSpiRegistryEventTracker(_a11yConnection);
            _ = InitializeRegistryTrackerAsync(_registryTracker);
        }

        /// <summary>
        /// Adds a window to the AT-SPI tree.
        /// </summary>
        public void AddWindow(AutomationPeer windowPeer)
        {
            if (_a11yConnection is null || _appRoot is null)
                return;

            // Idempotent check
            if (TryGetAttachedNode(windowPeer) is RootAtSpiNode)
                return;

            if (GetOrCreateNode(windowPeer) is not RootAtSpiNode windowNode)
                return;

            windowNode.AppRoot = _appRoot;
            if (!AttachNode(windowNode, parent: null))
                return;

            if (!_appRoot.WindowChildren.Contains(windowNode))
                _appRoot.AddWindowChild(windowNode);

            var isEmbedded = false;
            lock (_embedSync)
                isEmbedded = _isEmbedded;

            // GTK-like root registration behavior:
            // embed once, then only emit incremental children-changed for later windows.
            if (isEmbedded)
            {
                EmitWindowChildAdded(windowNode);
            }
            else
            {
                // Embed may already be in flight from StartAsync. The embed completion path
                // emits children-changed for all currently tracked windows.
                _ = EnsureEmbeddedAndAnnounceAsync();
            }
        }

        private Task EnsureEmbeddedAndAnnounceAsync()
        {
            lock (_embedSync)
            {
                if (_isEmbedded)
                    return Task.CompletedTask;

                // Ignore repeated embed requests while one is already in flight.
                if (_embedTask is { IsCompleted: false })
                    return _embedTask;

                _embedTask = EmbedAndAnnounceOnceAsync();
                return _embedTask;
            }
        }

        private async Task EmbedAndAnnounceOnceAsync()
        {
            try
            {
                await EmbedApplicationAsync();
            }
            catch (Exception e)
            {
                // Embed failed - screen reader won't discover us.
                // Reset so the next AddWindow retries.
                Logger.TryGet(LogEventLevel.Warning, LogArea.FreeDesktopPlatform)?
                    .Log(this, "AT-SPI embed failed; will retry when windows are added: {0}", e);
                lock (_embedSync)
                    _embedTask = null;
                return;
            }

            lock (_embedSync)
            {
                _isEmbedded = true;
                _embedTask = null;
            }

            // Now that the screen reader knows about us, emit children-changed
            // for every window that was added before the embed completed.
            if (!HasEventListeners || _appRootEventHandler is not { } eventHandler || _appRoot is null)
                return;

            var children = _appRoot.WindowChildren.ToArray();
            for (var i = 0; i < children.Length; i++)
            {
                var childRef = GetReference(children[i]);
                var childVariant = new DBusVariant(childRef.ToDbusStruct());
                eventHandler.EmitChildrenChangedSignal("add", i, childVariant);
            }
        }

        private void EmitWindowChildAdded(RootAtSpiNode windowNode)
        {
            if (!HasEventListeners || _appRootEventHandler is not { } eventHandler) return;
            var childRef = GetReference(windowNode);
            var childVariant = new DBusVariant(childRef.ToDbusStruct());
            eventHandler.EmitChildrenChangedSignal(
                "add", _appRoot!.WindowChildren.Count - 1, childVariant);
        }

        /// <summary>
        /// Removes a window from the AT-SPI tree.
        /// </summary>
        public void RemoveWindow(AutomationPeer windowPeer)
        {
            if (_a11yConnection is null || _appRoot is null)
                return;

            var windowNode = TryGetAttachedNode(windowPeer) as RootAtSpiNode;
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

            DetachSubtreeRecursive(windowNode);
        }

        public async ValueTask DisposeAsync()
        {
            lock (_embedSync)
            {
                _isEmbedded = false;
                _embedTask = null;
            }

            _registryTracker?.Dispose();
            _registryTracker = null;

            var nodes = _nodesByPath.Values.ToArray();

            // Dispose all D-Bus registrations before the connection.
            foreach (var node in nodes)
            {
                await node.DisposePathRegistrationAsync();
            }

            _appRootRegistration?.Dispose();
            _appRootRegistration = null;
            _cacheRegistration?.Dispose();
            _cacheRegistration = null;

            if (_a11yConnection is not null)
            {
                await _a11yConnection.DisposeAsync();
                _a11yConnection = null;
            }

            foreach (var node in nodes)
                ReleaseNode(node);

            _uniqueName = string.Empty;
            _cacheHandler = null;
            _appRoot = null;
            _appRootEventHandler = null;
            _nodesByPath.Clear();
            _nodesByPeer.Clear();
        }

        internal AtSpiObjectReference GetReference(AtSpiNode? node)
        {
            if (node is null || !node.IsAttached || !_nodesByPath.ContainsKey(node.Path))
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

        internal void EmitWindowActivationChange(RootAtSpiNode windowNode, bool active)
        {
            if (_a11yConnection is null || !HasEventListeners)
                return;

            if (windowNode.EventObjectHandler is { } eventHandler)
                eventHandler.EmitStateChangedSignal("active", active ? 1 : 0, new DBusVariant(0));

            if (windowNode.EventWindowHandler is not { } windowHandler)
                return;

            if (active)
                windowHandler.EmitActivateSignal();
            else
                windowHandler.EmitDeactivateSignal();
        }

        internal void EmitFocusChange(AtSpiNode? focusedNode)
        {
            if (_a11yConnection is null || focusedNode is null || !HasEventListeners)
                return;

            if (focusedNode.EventObjectHandler is { } eventHandler)
                eventHandler.EmitStateChangedSignal("focused", 1, new DBusVariant(0));
        }

        internal string AllocateNodePath()
        {
            return $"{AppPathPrefix}/{Interlocked.Increment(ref _nextNodeId)}";
        }

        internal AtSpiNode GetOrCreateNode(AutomationPeer peer)
        {
            if (_nodesByPeer.TryGetValue(peer, out var node))
                return node;

            node = AtSpiNode.Create(peer, this);
            _nodesByPeer[peer] = node;
            return node;
        }

        internal AtSpiNode? TryGetNode(AutomationPeer? peer)
        {
            if (peer is null)
                return null;

            _nodesByPeer.TryGetValue(peer, out var node);
            return node;
        }

        internal AtSpiNode? TryGetAttachedNode(AutomationPeer? peer)
        {
            var node = TryGetNode(peer);
            return node is { IsAttached: true } && _nodesByPath.ContainsKey(node.Path) ? node : null;
        }

        internal bool AttachNode(AtSpiNode node, AtSpiNode? parent)
        {
            if (_a11yConnection is null)
                return false;

            if (parent is not null && !parent.IsAttached)
                return false;

            if (node.IsAttached)
            {
                if (!ReferenceEquals(node.Parent, parent))
                {
                    node.Parent?.RemoveAttachedChild(node);
                    node.SetParent(parent);
                }

                node.BuildAndRegisterHandlers(_a11yConnection, _syncContext);
                _nodesByPath[node.Path] = node;
                return true;
            }

            node.Attach(parent);
            if (!node.IsAttached)
                return false;

            _nodesByPath[node.Path] = node;
            return true;
        }

        internal void DetachSubtreeRecursive(AtSpiNode rootNode)
        {
            var toRemove = new List<AtSpiNode>();
            CollectSubtree(rootNode, toRemove);
            RemoveNodes(toRemove, emitDefunct: true);
        }

        private static void CollectSubtree(AtSpiNode node, List<AtSpiNode> result)
        {
            foreach (var child in node.AttachedChildren.ToArray())
                CollectSubtree(child, result);

            result.Add(node);
        }

        private void RemoveNodes(IEnumerable<AtSpiNode> nodes, bool emitDefunct)
        {
            foreach (var node in nodes)
            {
                if (!node.IsAttached || !_nodesByPath.ContainsKey(node.Path))
                    continue;

                if (emitDefunct && HasEventListeners && node.EventObjectHandler is { } eventHandler)
                    eventHandler.EmitStateChangedSignal("defunct", 1, new DBusVariant("0"));

                _nodesByPath.Remove(node.Path);
                node.Parent?.RemoveAttachedChild(node);

                if (node is RootAtSpiNode rootNode)
                    _appRoot?.RemoveWindowChild(rootNode);

                ReleaseNode(node);
            }
        }

        private async Task BuildAndRegisterAppRootAsync()
        {
            if (_a11yConnection is null || _appRoot is null)
                return;

            _appRootRegistration?.Dispose();
            _appRootRegistration = null;

            var accessibleHandler = new ApplicationAccessibleHandler(this, _appRoot);
            var applicationHandler = new ApplicationNodeApplicationHandler();
            var eventHandler = new AtSpiEventObjectHandler(this, _appRoot.Path);
            _appRootEventHandler = eventHandler;

            var targets = new List<object> { accessibleHandler, applicationHandler, eventHandler };
            _appRootRegistration = await _a11yConnection.RegisterObjects(
                (DBusObjectPath)RootPath,
                targets,
                _syncContext);
        }

        private async Task RegisterCachePathAsync()
        {
            if (_a11yConnection is null || _cacheHandler is null)
                return;

            _cacheRegistration?.Dispose();
            _cacheRegistration = null;

            _cacheRegistration = await _a11yConnection.RegisterObjects(
                (DBusObjectPath)CachePath,
                [_cacheHandler],
                _syncContext);
        }

        private async Task<string> GetAccessibilityBusAddressAsync()
        {
            try
            {
                await using var connection = await DBusConnection.ConnectSessionAsync();
                var proxy = new OrgA11yBusProxy(connection, BusNameA11y, new DBusObjectPath(PathA11y));
                return await proxy.GetAddressAsync();
            }
            catch (Exception e)
            {
                Logger.TryGet(LogEventLevel.Debug, LogArea.FreeDesktopPlatform)?
                    .Log(this, "Failed to resolve AT-SPI accessibility bus address: {0}", e);
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

        private static async Task InitializeRegistryTrackerAsync(AtSpiRegistryEventTracker tracker)
        {
            try
            {
                await tracker.InitializeAsync();
            }
            catch (Exception e)
            {
                // Registry tracking is best-effort; AT-SPI server remains functional without it.
                Logger.TryGet(LogEventLevel.Debug, LogArea.FreeDesktopPlatform)?
                    .Log(tracker, "AT-SPI registry listener tracking initialization failed: {0}", e);
            }
        }

        private void ReleaseNode(AtSpiNode node)
        {
            node.Detach();
            _nodesByPeer.Remove(node.Peer);
        }
    }
}
