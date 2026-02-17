using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.FreeDesktop.AtSpi.DBusXml;
using static Avalonia.FreeDesktop.AtSpi.AtSpiConstants;

namespace Avalonia.FreeDesktop.AtSpi.Handlers
{
    internal sealed class AtSpiAccessibleHandler : IOrgA11yAtspiAccessible
    {
        private readonly AtSpiServer _server;
        private readonly AtSpiNode _node;

        public AtSpiAccessibleHandler(AtSpiServer server, AtSpiNode node)
        {
            _server = server;
            _node = node;
        }

        public uint Version => AccessibleVersion;

        public string Name => _node.InvokeSync(() => _node.Peer.GetName());

        public string Description => _node.InvokeSync(() => _node.Peer.GetHelpText());

        public AtSpiObjectReference Parent
        {
            get
            {
                var parent = _node.InvokeSync(() => _node.Peer.GetParent());
                var parentNode = AtSpiNode.TryGet(parent);
                return _server.GetReference(parentNode);
            }
        }

        public int ChildCount => _node.InvokeSync(() => _node.Peer.GetChildren().Count);

        public string Locale => ResolveLocale();

        public string AccessibleId => _node.InvokeSync(() => _node.Peer.GetAutomationId() ?? string.Empty);

        public string HelpText => _node.InvokeSync(() => _node.Peer.GetHelpText());

        public ValueTask<AtSpiObjectReference> GetChildAtIndexAsync(int index)
        {
            var children = _node.InvokeSync(() => _node.Peer.GetChildren());
            if (index >= 0 && index < children.Count)
            {
                var childNode = AtSpiNode.GetOrCreate(children[index], _server);
                return ValueTask.FromResult(_server.GetReference(childNode));
            }

            return ValueTask.FromResult(_server.GetNullReference());
        }

        public ValueTask<List<AtSpiObjectReference>> GetChildrenAsync()
        {
            var children = _node.InvokeSync(() => _node.Peer.GetChildren());
            var refs = new List<AtSpiObjectReference>(children.Count);
            foreach (var child in children)
            {
                var childNode = AtSpiNode.GetOrCreate(child, _server);
                refs.Add(_server.GetReference(childNode));
            }

            return ValueTask.FromResult(refs);
        }

        public ValueTask<int> GetIndexInParentAsync()
        {
            var index = _node.InvokeSync(() =>
            {
                var parent = _node.Peer.GetParent();
                if (parent is null)
                    return -1;
                var siblings = parent.GetChildren();
                for (var i = 0; i < siblings.Count; i++)
                {
                    if (ReferenceEquals(siblings[i], _node.Peer))
                        return i;
                }

                return -1;
            });
            return ValueTask.FromResult(index);
        }

        public ValueTask<List<AtSpiRelationEntry>> GetRelationSetAsync()
        {
            return ValueTask.FromResult(new List<AtSpiRelationEntry>());
        }

        public ValueTask<uint> GetRoleAsync()
        {
            var role = _node.InvokeSync(() => AtSpiNode.ToAtSpiRole(_node.Peer.GetAutomationControlType()));
            return ValueTask.FromResult((uint)role);
        }

        public ValueTask<string> GetRoleNameAsync()
        {
            var role = _node.InvokeSync(() => AtSpiNode.ToAtSpiRole(_node.Peer.GetAutomationControlType()));
            return ValueTask.FromResult(AtSpiNode.ToAtSpiRoleName(role));
        }

        public ValueTask<string> GetLocalizedRoleNameAsync()
        {
            var role = _node.InvokeSync(() => AtSpiNode.ToAtSpiRole(_node.Peer.GetAutomationControlType()));
            return ValueTask.FromResult(AtSpiNode.ToAtSpiRoleName(role));
        }

        public ValueTask<List<uint>> GetStateAsync()
        {
            return ValueTask.FromResult(_node.ComputeStates());
        }

        public ValueTask<AtSpiAttributeSet> GetAttributesAsync()
        {
            return ValueTask.FromResult(new AtSpiAttributeSet());
        }

        public ValueTask<AtSpiObjectReference> GetApplicationAsync()
        {
            return ValueTask.FromResult(_server.GetRootReference());
        }

        public ValueTask<List<string>> GetInterfacesAsync()
        {
            var interfaces = _node.InvokeSync(() => _node.GetSupportedInterfaces());
            var sorted = interfaces.OrderBy(static i => i, StringComparer.Ordinal).ToList();
            return ValueTask.FromResult(sorted);
        }
    }
}
