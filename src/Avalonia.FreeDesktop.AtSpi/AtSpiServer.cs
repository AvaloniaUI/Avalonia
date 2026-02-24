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
        private readonly object _embedSync = new();

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

            // Create a SynchronizationContext that dispatches to the Avalonia UI thread.
            _syncContext = new AvaloniaSynchronizationContext(DispatcherPriority.Normal);

            // Get a11y bus address
            var address = await GetAccessibilityBusAddressAsync();
            
            if (string.IsNullOrWhiteSpace(address))
                throw new InvalidOperationException("Failed to resolve the accessibility bus address.");

            // Connect to a11y bus
            _a11yConnection = await DBusConnection.ConnectAsync(address);
            _uniqueName = await _a11yConnection.GetUniqueNameAsync() ?? string.Empty;

            // Create detached application root
            _appRoot = new ApplicationAtSpiNode(null);

            // Build and register handlers for app root
            _cacheHandler = new AtSpiCacheHandler();
            await BuildAndRegisterAppRootAsync();

            // Register cache handler path
            await RegisterCachePathAsync();

            // Start tracking registry event listeners in the background so we don't
            // delay AT-SPI root readiness and initial embed timing.
            _registryTracker = new AtSpiRegistryEventTracker(_a11yConnection);
            _ = InitializeRegistryTrackerAsync(_registryTracker);

            // Attempt embed immediately so AT clients discover us as early as possible.
            // If this fails, AddWindow keeps retrying through EnsureEmbeddedAndAnnounceAsync.
            _ = EnsureEmbeddedAndAnnounceAsync();
        }

        /// <summary>
        /// Adds a window to the AT-SPI tree.
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
                // Embed failed — screen reader won't discover us.
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

            RemoveSubtreeRecursive(windowNode);
        }

        /// <summary>
        /// Ensures a node is registered in the tree (idempotent).
        /// Called after <see cref="AtSpiNode.GetOrCreate"/> to lazily register new nodes.
        /// Re-registers the D-Bus path with updated handlers in case the peer's
        /// supported interfaces have changed since initial registration.
        /// </summary>
        internal void EnsureNodeRegistered(AtSpiNode node)
        {
            if (_nodesByPath.ContainsKey(node.Path))
            {
                if (_a11yConnection is not null)
                    node.BuildAndRegisterHandlers(_a11yConnection, _syncContext);
                return;
            }

            RegisterNodeInternal(node);
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

            // Dispose all D-Bus registrations before the connection
            foreach (var node in _nodesByPath.Values)
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
            {
                eventHandler.EmitStateChangedSignal("focused", 1, new DBusVariant(0));
            }
        }

        // Node registration

        /// <summary>
        /// Registers a node in _nodesByPath, builds handlers, and registers D-Bus path.
        /// </summary>
        private void RegisterNodeInternal(AtSpiNode node)
        {
            _nodesByPath[node.Path] = node;
            TrackChildInParent(node);
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

        private void TrackChildInParent(AtSpiNode childNode)
        {
            try
            {
                var parentPeer = childNode.Peer.GetParent();
                if (parentPeer is null)
                    return;

                var parentNode = AtSpiNode.TryGet(parentPeer);
                if (parentNode is not null && _nodesByPath.ContainsKey(parentNode.Path))
                    parentNode.AddRegisteredChild(childNode);
            }
            catch (Exception e)
            {
                // Parent peer may be defunct
                Logger.TryGet(LogEventLevel.Debug, LogArea.FreeDesktopPlatform)?
                    .Log(childNode, "AT-SPI parent tracking skipped due to defunct parent peer: {0}", e);
            }
        }

        private void RemoveFromParentTracking(AtSpiNode node)
        {
            try
            {
                var parentPeer = node.Peer.GetParent();
                if (parentPeer is not null)
                    AtSpiNode.TryGet(parentPeer)?.RemoveRegisteredChild(node);
            }
            catch (Exception e)
            {
                // Parent peer may be defunct
                Logger.TryGet(LogEventLevel.Debug, LogArea.FreeDesktopPlatform)?
                    .Log(node, "AT-SPI parent untracking skipped due to defunct parent peer: {0}", e);
            }
        }

        internal void RemoveSubtreeRecursive(AtSpiNode rootNode)
        {
            var toRemove = new List<AtSpiNode>();
            CollectSubtree(rootNode, toRemove);
            RemoveNodes(toRemove, emitDefunct: true);
        }

        private static void CollectSubtree(AtSpiNode node, List<AtSpiNode> result)
        {
            if (node.HasRegisteredChildren)
            {
                foreach (var child in node.RegisteredChildren)
                    CollectSubtree(child, result);
            }

            result.Add(node);
        }

        private void RemoveNodes(IEnumerable<AtSpiNode> nodes, bool emitDefunct)
        {
            foreach (var node in nodes)
            {
                if (!_nodesByPath.ContainsKey(node.Path))
                    continue;

                if (emitDefunct && HasEventListeners && node.EventObjectHandler is { } eventHandler)
                    eventHandler.EmitStateChangedSignal("defunct", 1, new DBusVariant("0"));

                _nodesByPath.Remove(node.Path);
                RemoveFromParentTracking(node);

                if (node is RootAtSpiNode rootNode)
                    _appRoot?.RemoveWindowChild(rootNode);

                ReleaseNode(node);
            }
        }

        private static void ReleaseNode(AtSpiNode node)
        {
            node.Detach();
            AtSpiNode.Release(node.Peer);
        }

    }
}
