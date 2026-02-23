using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.DBus;
using Avalonia.FreeDesktop.AtSpi.DBusXml;
using static Avalonia.FreeDesktop.AtSpi.AtSpiConstants;

namespace Avalonia.FreeDesktop.AtSpi.Handlers
{
    /// <summary>
    /// Implements the AT-SPI Accessible interface for an AutomationPeer-backed node.
    /// </summary>
    internal sealed class AtSpiAccessibleHandler(AtSpiServer server, AtSpiNode node) : IOrgA11yAtspiAccessible
    {
        public uint Version => AccessibleVersion;

        public string Name => AtSpiNode.GetAccessibleName(node.Peer);

        public string Description => node.Peer.GetHelpText();

        public AtSpiObjectReference Parent
        {
            get
            {
                // For window nodes, return the ApplicationAtSpiNode as parent
                if (node is RootAtSpiNode { AppRoot: { } appRoot })
                    return new AtSpiObjectReference(
                        server.UniqueName, new DBusObjectPath(appRoot.Path));

                var parent = node.Peer.GetParent();
                var parentNode = AtSpiNode.TryGet(parent);
                return server.GetReference(parentNode);
            }
        }

        public int ChildCount => node.Peer.GetChildren().Count;

        public string Locale => ResolveLocale();

        public string AccessibleId => node.Peer.GetAutomationId() ?? string.Empty;

        public string HelpText => node.Peer.GetHelpText();

        public ValueTask<AtSpiObjectReference> GetChildAtIndexAsync(int index)
        {
            var children = node.Peer.GetChildren();
            
            if (index < 0 || index >= children.Count) 
                return ValueTask.FromResult(server.GetNullReference());
            
            var childNode = AtSpiNode.GetOrCreate(children[index], server);
            if (childNode is null)
                return ValueTask.FromResult(server.GetNullReference());
            server.EnsureNodeRegistered(childNode);
            return ValueTask.FromResult(server.GetReference(childNode));

        }

        public ValueTask<List<AtSpiObjectReference>> GetChildrenAsync()
        {
            var children = node.Peer.GetChildren();
            var refs = new List<AtSpiObjectReference>(children.Count);
            foreach (var child in children)
            {
                var childNode = AtSpiNode.GetOrCreate(child, server);
                if (childNode is null)
                    continue;
                server.EnsureNodeRegistered(childNode);
                refs.Add(server.GetReference(childNode));
            }

            return ValueTask.FromResult(refs);
        }

        public ValueTask<int> GetIndexInParentAsync()
        {
            var parent = node.Peer.GetParent();
            if (parent is null)
                return ValueTask.FromResult(-1);
            var siblings = parent.GetChildren();
            for (var i = 0; i < siblings.Count; i++)
            {
                if (ReferenceEquals(siblings[i], node.Peer))
                    return ValueTask.FromResult(i);
            }

            return ValueTask.FromResult(-1);
        }

        public ValueTask<List<AtSpiRelationEntry>> GetRelationSetAsync()
        {
            var relations = new List<AtSpiRelationEntry>();

            var labeledBy = node.Peer.GetLabeledBy();
            if (labeledBy is not null)
            {
                var labelNode = AtSpiNode.GetOrCreate(labeledBy, server);
                if (labelNode is not null)
                {
                    server.EnsureNodeRegistered(labelNode);
                    // Relation type 2 = LABELLED_BY
                    relations.Add(new AtSpiRelationEntry(2, [server.GetReference(labelNode)]));
                }
            }

            return ValueTask.FromResult(relations);
        }

        public ValueTask<uint> GetRoleAsync()
        {
            var role = AtSpiNode.ToAtSpiRole(node.Peer.GetAutomationControlType(), node.Peer);
            return ValueTask.FromResult((uint)role);
        }

        public ValueTask<string> GetRoleNameAsync()
        {
            var role = AtSpiNode.ToAtSpiRole(node.Peer.GetAutomationControlType(), node.Peer);
            return ValueTask.FromResult(AtSpiNode.ToAtSpiRoleName(role));
        }

        public ValueTask<string> GetLocalizedRoleNameAsync()
        {
            var role = AtSpiNode.ToAtSpiRole(node.Peer.GetAutomationControlType(), node.Peer);
            return ValueTask.FromResult(AtSpiNode.ToAtSpiRoleName(role));
        }

        public ValueTask<List<uint>> GetStateAsync()
        {
            return ValueTask.FromResult(node.ComputeStates());
        }

        public ValueTask<AtSpiAttributeSet> GetAttributesAsync()
        {
            var attrs = new AtSpiAttributeSet { ["toolkit"] = "Avalonia" };

            var name = node.Peer.GetName();
            if (!string.IsNullOrEmpty(name))
                attrs["explicit-name"] = "true";

            var acceleratorKey = node.Peer.GetAcceleratorKey();
            if (!string.IsNullOrEmpty(acceleratorKey))
                attrs["accelerator-key"] = acceleratorKey;

            var accessKey = node.Peer.GetAccessKey();
            if (!string.IsNullOrEmpty(accessKey))
                attrs["access-key"] = accessKey;

            return ValueTask.FromResult(attrs);
        }

        public ValueTask<AtSpiObjectReference> GetApplicationAsync()
        {
            return ValueTask.FromResult(server.GetRootReference());
        }

        public ValueTask<List<string>> GetInterfacesAsync()
        {
            var interfaces = node.GetSupportedInterfaces();
            var sorted = interfaces.OrderBy(static i => i, StringComparer.Ordinal).ToList();
            return ValueTask.FromResult(sorted);
        }
    }
}
