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

                return server.GetReference(node.Parent);
            }
        }

        public int ChildCount => node.EnsureChildren().Count;

        public string Locale => ResolveLocale();

        public string AccessibleId => node.Peer.GetAutomationId() ?? string.Empty;

        public string HelpText => node.Peer.GetHelpText();

        public ValueTask<AtSpiObjectReference> GetChildAtIndexAsync(int index)
        {
            var children = node.EnsureChildren();
            if (index < 0 || index >= children.Count)
                return ValueTask.FromResult(server.GetNullReference());

            return ValueTask.FromResult(server.GetReference(children[index]));
        }

        public ValueTask<List<AtSpiObjectReference>> GetChildrenAsync()
        {
            var children = node.EnsureChildren();
            var refs = new List<AtSpiObjectReference>(children.Count);
            foreach (var child in children)
            {
                refs.Add(server.GetReference(child));
            }

            return ValueTask.FromResult(refs);
        }

        public ValueTask<int> GetIndexInParentAsync()
        {
            // Window nodes are children of the ApplicationAtSpiNode, but their
            // internal Parent field is null (they are attached with parent: null).
            // Mirror the Parent property's special case so that backward path
            // walks (e.g. accerciser's get_index_in_parent) work correctly.
            if (node is RootAtSpiNode { AppRoot: { } appRoot })
            {
                var windows = appRoot.WindowChildren;
                for (var i = 0; i < windows.Count; i++)
                {
                    if (ReferenceEquals(windows[i], node))
                        return ValueTask.FromResult(i);
                }

                return ValueTask.FromResult(-1);
            }

            var parent = node.Parent;
            if (parent is null)
                return ValueTask.FromResult(-1);

            var siblings = parent.EnsureChildren();
            for (var i = 0; i < siblings.Count; i++)
            {
                if (ReferenceEquals(siblings[i], node))
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
                var labelNode = server.TryGetAttachedNode(labeledBy);
                if (labelNode is not null)
                {
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
