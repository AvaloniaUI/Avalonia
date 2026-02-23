using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.DBus;
using Avalonia.FreeDesktop.AtSpi.DBusXml;
using static Avalonia.FreeDesktop.AtSpi.AtSpiConstants;

namespace Avalonia.FreeDesktop.AtSpi.Handlers
{
    /// <summary>
    /// Implements the AT-SPI Selection interface for list-like containers.
    /// </summary>
    internal sealed class AtSpiSelectionHandler(AtSpiServer server, AtSpiNode node) : IOrgA11yAtspiSelection
    {
        public uint Version => SelectionVersion;

        public int NSelectedChildren
        {
            get
            {
                var provider = node.Peer.GetProvider<ISelectionProvider>();
                return provider?.GetSelection().Count ?? 0;
            }
        }

        public ValueTask<AtSpiObjectReference> GetSelectedChildAsync(int selectedChildIndex)
        {
            var provider = node.Peer.GetProvider<ISelectionProvider>();
            if (provider is null)
                return ValueTask.FromResult(server.GetNullReference());

            var selection = provider.GetSelection();
            if (selectedChildIndex < 0 || selectedChildIndex >= selection.Count)
                return ValueTask.FromResult(server.GetNullReference());

            var selectedPeer = selection[selectedChildIndex];
            var childNode = AtSpiNode.GetOrCreate(selectedPeer, server);
            if (childNode is null)
                return ValueTask.FromResult(server.GetNullReference());
            server.EnsureNodeRegistered(childNode);
            return ValueTask.FromResult(server.GetReference(childNode));
        }

        public ValueTask<bool> SelectChildAsync(int childIndex)
        {
            var children = node.Peer.GetChildren();
            if (childIndex < 0 || childIndex >= children.Count)
                return ValueTask.FromResult(false);

            var childPeer = children[childIndex];
            if (childPeer.GetProvider<ISelectionItemProvider>() is not { } selectionItem)
                return ValueTask.FromResult(false);

            selectionItem.Select();
            return ValueTask.FromResult(true);
        }

        public ValueTask<bool> DeselectSelectedChildAsync(int selectedChildIndex)
        {
            var provider = node.Peer.GetProvider<ISelectionProvider>();
            if (provider is null)
                return ValueTask.FromResult(false);

            var selection = provider.GetSelection();
            if (selectedChildIndex < 0 || selectedChildIndex >= selection.Count)
                return ValueTask.FromResult(false);

            var selectedPeer = selection[selectedChildIndex];
            if (selectedPeer.GetProvider<ISelectionItemProvider>() is not { } selectionItem)
                return ValueTask.FromResult(false);

            selectionItem.RemoveFromSelection();
            return ValueTask.FromResult(true);
        }

        public ValueTask<bool> IsChildSelectedAsync(int childIndex)
        {
            var children = node.Peer.GetChildren();
            if (childIndex < 0 || childIndex >= children.Count)
                return ValueTask.FromResult(false);

            var childPeer = children[childIndex];
            if (childPeer.GetProvider<ISelectionItemProvider>() is not { } selectionItem)
                return ValueTask.FromResult(false);

            return ValueTask.FromResult(selectionItem.IsSelected);
        }

        public ValueTask<bool> SelectAllAsync()
        {
            var provider = node.Peer.GetProvider<ISelectionProvider>();
            if (provider is null || !provider.CanSelectMultiple)
                return ValueTask.FromResult(false);

            var children = node.Peer.GetChildren();
            foreach (var child in children)
            {
                if (child.GetProvider<ISelectionItemProvider>() is { } selectionItem)
                    selectionItem.AddToSelection();
            }

            return ValueTask.FromResult(true);
        }

        public ValueTask<bool> ClearSelectionAsync()
        {
            var provider = node.Peer.GetProvider<ISelectionProvider>();
            if (provider is null)
                return ValueTask.FromResult(false);

            var selection = provider.GetSelection();
            foreach (var selectedPeer in selection)
            {
                if (selectedPeer.GetProvider<ISelectionItemProvider>() is { } selectionItem)
                    selectionItem.RemoveFromSelection();
            }

            return ValueTask.FromResult(true);
        }

        public ValueTask<bool> DeselectChildAsync(int childIndex)
        {
            var children = node.Peer.GetChildren();
            if (childIndex < 0 || childIndex >= children.Count)
                return ValueTask.FromResult(false);

            var childPeer = children[childIndex];
            if (childPeer.GetProvider<ISelectionItemProvider>() is not { } selectionItem)
                return ValueTask.FromResult(false);

            selectionItem.RemoveFromSelection();
            return ValueTask.FromResult(true);
        }
    }
}
