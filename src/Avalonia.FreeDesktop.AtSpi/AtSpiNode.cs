using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Threading;
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
            _path = $"{AppPathPrefix}/{s_nextId++}";
            s_nodes.Add(peer, this);
            peer.ChildrenChanged += OnPeerChildrenChanged;
            peer.PropertyChanged += OnPeerPropertyChanged;
        }

        protected AtSpiNode(AutomationPeer peer, AtSpiServer server, string path)
        {
            Peer = peer;
            Server = server;
            _path = path;
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

            // All visual elements support Component
            interfaces.Add(IfaceComponent);

            if (Peer.GetProvider<IInvokeProvider>() is not null ||
                Peer.GetProvider<IToggleProvider>() is not null ||
                Peer.GetProvider<IExpandCollapseProvider>() is not null)
            {
                interfaces.Add(IfaceAction);
            }

            if (Peer.GetProvider<IRangeValueProvider>() is not null)
                interfaces.Add(IfaceValue);

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

        internal void InvokeSync(Action action)
        {
            if (Dispatcher.UIThread.CheckAccess())
                action();
            else
                Dispatcher.UIThread.InvokeAsync(action).Wait();
        }

        internal T InvokeSync<T>(Func<T> func)
        {
            if (Dispatcher.UIThread.CheckAccess())
                return func();
            else
                return Dispatcher.UIThread.InvokeAsync(func).Result;
        }

        internal TResult InvokeSync<TInterface, TResult>(Func<TInterface, TResult> func)
        {
            if (Peer.GetProvider<TInterface>() is TInterface provider)
                return InvokeSync(() => func(provider));
            throw new NotSupportedException($"Peer does not implement {typeof(TInterface).Name}.");
        }

        internal void InvokeSync<TInterface>(Action<TInterface> action)
        {
            if (Peer.GetProvider<TInterface>() is TInterface provider)
                InvokeSync(() => action(provider));
            else
                throw new NotSupportedException($"Peer does not implement {typeof(TInterface).Name}.");
        }

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
