using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using static Avalonia.FreeDesktop.AtSpi.AtSpiConstants;

namespace Avalonia.FreeDesktop.AtSpi
{
    internal partial class AtSpiNode
    {
        private static readonly ConditionalWeakTable<AutomationPeer, AtSpiNode> s_nodes = new();
        private static int s_nextId;

        private readonly string _path;

        public AtSpiNode(AutomationPeer peer, AtSpiServer server)
        {
            Peer = peer;
            Server = server;
            _path = $"{AppPathPrefix}/{Interlocked.Increment(ref s_nextId)}";
            s_nodes.Add(peer, this);
            peer.ChildrenChanged += OnPeerChildrenChanged;
            peer.PropertyChanged += OnPeerPropertyChanged;
        }

        public AutomationPeer Peer { get; }
        public AtSpiServer Server { get; }
        public string Path => _path;
        public AtSpiNodeHandlers? Handlers { get; set; }

        public HashSet<string> GetSupportedInterfaces()
        {
            var interfaces = new HashSet<string>(StringComparer.Ordinal) { IfaceAccessible };

            if (Peer.GetProvider<IRootProvider>() is not null)
                interfaces.Add(IfaceApplication);

            // All visual elements support Component and Collection
            interfaces.Add(IfaceComponent);
            interfaces.Add(IfaceCollection);

            if (Peer.GetProvider<IInvokeProvider>() is not null ||
                Peer.GetProvider<IToggleProvider>() is not null ||
                Peer.GetProvider<IExpandCollapseProvider>() is not null ||
                Peer.GetProvider<IScrollProvider>() is not null ||
                // TODO: expose Action for ISelectionItemProvider so that selectable items
                // (e.g. TabItem, ListBoxItem) can be activated via AT-SPI. Ideally the core
                // automation peers would implement IInvokeProvider and/or the parent container
                // would implement ISelectionProvider, making this unnecessary.
                Peer.GetProvider<ISelectionItemProvider>() is not null)
            {
                interfaces.Add(IfaceAction);
            }

            if (Peer.GetProvider<IRangeValueProvider>() is not null)
                interfaces.Add(IfaceValue);

            if (Peer.GetProvider<ISelectionProvider>() is not null)
                interfaces.Add(IfaceSelection);

            // Only expose Text/EditableText when IValueProvider is present but IRangeValueProvider is not.
            // Peers with IRangeValueProvider (sliders, progress bars) use the Value AT-SPI interface instead.
            if (Peer.GetProvider<IValueProvider>() is { } valueProvider
                && Peer.GetProvider<IRangeValueProvider>() is null)
            {
                interfaces.Add(IfaceText);
                if (!valueProvider.IsReadOnly)
                    interfaces.Add(IfaceEditableText);
            }

            if (Peer.GetAutomationControlType() == AutomationControlType.Image)
                interfaces.Add(IfaceImage);

            return interfaces;
        }

#if NET6_0_OR_GREATER
        [return: NotNullIfNotNull(nameof(peer))]
#endif
        public static AtSpiNode? GetOrCreate(AutomationPeer? peer, AtSpiServer server)
        {
            if (peer is null)
                return null;

            return s_nodes.GetValue(peer, p =>
            {
                if (p.GetProvider<IRootProvider>() is not null)
                    return new RootAtSpiNode(p, server);
                return new AtSpiNode(p, server);
            });
        }

        public static AtSpiNode? TryGet(AutomationPeer? peer)
        {
            if (peer is null)
                return null;
            s_nodes.TryGetValue(peer, out var node);
            return node;
        }

        public static void Release(AutomationPeer peer) => s_nodes.Remove(peer);

        private void OnPeerChildrenChanged(object? sender, EventArgs e)
        {
            Server.EmitChildrenChanged(this);
        }

        private void OnPeerPropertyChanged(object? sender, AutomationPropertyChangedEventArgs e)
        {
            Server.EmitPropertyChange(this, e);
        }
    }
}
