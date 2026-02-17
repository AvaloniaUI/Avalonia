using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.DBus;
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

        public string Name => _node.Peer.GetName();

        public string Description => _node.Peer.GetHelpText();

        public AtSpiObjectReference Parent
        {
            get
            {
                // For window nodes, return the ApplicationAtSpiNode as parent
                if (_node is RootAtSpiNode { AppRoot: { } appRoot })
                    return new AtSpiObjectReference(
                        _server.UniqueName, new DBusObjectPath(appRoot.Path));

                var parent = _node.Peer.GetParent();
                var parentNode = AtSpiNode.TryGet(parent);
                return _server.GetReference(parentNode);
            }
        }

        public int ChildCount => _node.Peer.GetChildren().Count;

        public string Locale => ResolveLocale();

        public string AccessibleId => _node.Peer.GetAutomationId() ?? string.Empty;

        public string HelpText => _node.Peer.GetHelpText();

        public ValueTask<AtSpiObjectReference> GetChildAtIndexAsync(int index)
        {
            var children = _node.Peer.GetChildren();
            if (index >= 0 && index < children.Count)
            {
                var childNode = AtSpiNode.GetOrCreate(children[index], _server);
                if (childNode is not null)
                    _server.EnsureNodeRegistered(childNode);
                return ValueTask.FromResult(_server.GetReference(childNode));
            }

            return ValueTask.FromResult(_server.GetNullReference());
        }

        public ValueTask<List<AtSpiObjectReference>> GetChildrenAsync()
        {
            var children = _node.Peer.GetChildren();
            var refs = new List<AtSpiObjectReference>(children.Count);
            foreach (var child in children)
            {
                var childNode = AtSpiNode.GetOrCreate(child, _server);
                if (childNode is not null)
                    _server.EnsureNodeRegistered(childNode);
                refs.Add(_server.GetReference(childNode));
            }

            return ValueTask.FromResult(refs);
        }

        public ValueTask<int> GetIndexInParentAsync()
        {
            var parent = _node.Peer.GetParent();
            if (parent is null)
                return ValueTask.FromResult(-1);
            var siblings = parent.GetChildren();
            for (var i = 0; i < siblings.Count; i++)
            {
                if (ReferenceEquals(siblings[i], _node.Peer))
                    return ValueTask.FromResult(i);
            }

            return ValueTask.FromResult(-1);
        }

        public ValueTask<List<AtSpiRelationEntry>> GetRelationSetAsync()
        {
            return ValueTask.FromResult(new List<AtSpiRelationEntry>());
        }

        public ValueTask<uint> GetRoleAsync()
        {
            var role = AtSpiNode.ToAtSpiRole(_node.Peer.GetAutomationControlType());
            return ValueTask.FromResult((uint)role);
        }

        public ValueTask<string> GetRoleNameAsync()
        {
            var role = AtSpiNode.ToAtSpiRole(_node.Peer.GetAutomationControlType());
            return ValueTask.FromResult(AtSpiNode.ToAtSpiRoleName(role));
        }

        public ValueTask<string> GetLocalizedRoleNameAsync()
        {
            var role = AtSpiNode.ToAtSpiRole(_node.Peer.GetAutomationControlType());
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
            var interfaces = _node.GetSupportedInterfaces();
            var sorted = interfaces.OrderBy(static i => i, StringComparer.Ordinal).ToList();
            return ValueTask.FromResult(sorted);
        }
    }
}
