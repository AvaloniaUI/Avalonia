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

        internal IReadOnlyList<AtSpiNode> EnsureChildren()
        {
            if (!IsAttached)
                return Array.Empty<AtSpiNode>();

            if (!_childrenDirty)
                return _attachedChildren;

            var childPeers = Peer.GetChildren();
            var nextChildren = new List<AtSpiNode>(childPeers.Count);
            var nextChildrenSet = new HashSet<AtSpiNode>();
            foreach (var childPeer in childPeers)
            {
                var childNode = Server.GetOrCreateNode(childPeer);
                if (!Server.AttachNode(childNode, this))
                    continue;

                nextChildren.Add(childNode);
                nextChildrenSet.Add(childNode);
            }

            if (_attachedChildren.Count > 0)
            {
                var removed = _attachedChildren.Where(c => !nextChildrenSet.Contains(c)).ToArray();
                foreach (var removedNode in removed)
                {
                    if (ReferenceEquals(removedNode.Parent, this))
                        Server.DetachSubtreeRecursive(removedNode);
                }
            }

            _attachedChildren = nextChildren;
            _childrenDirty = false;
            return _attachedChildren;
        }

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
                    .Log(null, "AT-SPI node path registration cleanup failed: {0}", e);
            }
        }

        private void OnPeerChildrenChanged(object? sender, EventArgs e)
        {
            if (Server.A11yConnection is null || !IsAttached)
                return;

            _childrenDirty = true;

            var childPeers = Peer.GetChildren();
            if (_attachedChildren.Count > 0)
            {
                var currentPeers = new HashSet<AutomationPeer>(childPeers);
                var removedChildren = _attachedChildren
                    .Where(childNode => !currentPeers.Contains(childNode.Peer))
                    .ToArray();

                foreach (var oldChild in removedChildren)
                {
                    if (ReferenceEquals(oldChild.Parent, this))
                        Server.DetachSubtreeRecursive(oldChild);
                }

                if (removedChildren.Length > 0)
                {
                    var removedSet = new HashSet<AtSpiNode>(removedChildren);
                    _attachedChildren = _attachedChildren
                        .Where(childNode => !removedSet.Contains(childNode))
                        .ToList();
                }
            }

            if (!Server.HasEventListeners || EventObjectHandler is not { } eventHandler) return;
            var reference = Server.GetReference(this);
            var childVariant = new DBusVariant(reference.ToDbusStruct());
            eventHandler.EmitChildrenChangedSignal("add", 0, childVariant);
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
