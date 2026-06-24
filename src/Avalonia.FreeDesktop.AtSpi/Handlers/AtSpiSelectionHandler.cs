using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.DBus;
using Avalonia.FreeDesktop.AtSpi.DBusXml;
using static Avalonia.FreeDesktop.AtSpi.AtSpiConstants;

namespace Avalonia.FreeDesktop.AtSpi.Handlers
{
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
            node.EnsureChildren();

            var provider = node.Peer.GetProvider<ISelectionProvider>();
            if (provider is null)
                return ValueTask.FromResult(server.GetNullReference());

            var selection = provider.GetSelection();
            if (selectedChildIndex < 0 || selectedChildIndex >= selection.Count)
                return ValueTask.FromResult(server.GetNullReference());

            var selectedPeer = selection[selectedChildIndex];
            var childNode = server.TryGetAttachedNode(selectedPeer);
            if (childNode is null)
                return ValueTask.FromResult(server.GetNullReference());

            return ValueTask.FromResult(server.GetReference(childNode));
        }

        public ValueTask<bool> SelectChildAsync(int childIndex)
        {
            var items = CollectSelectableItems(node.Peer);
            if (childIndex < 0 || childIndex >= items.Count)
                return ValueTask.FromResult(false);

            items[childIndex].AddToSelection();
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
            var items = CollectSelectableItems(node.Peer);
            if (childIndex < 0 || childIndex >= items.Count)
                return ValueTask.FromResult(false);

            return ValueTask.FromResult(items[childIndex].IsSelected);
        }

        public ValueTask<bool> SelectAllAsync()
        {
            var provider = node.Peer.GetProvider<ISelectionProvider>();
            if (provider is null || !provider.CanSelectMultiple)
                return ValueTask.FromResult(false);

            foreach (var item in CollectSelectableItems(node.Peer))
                item.AddToSelection();

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
            var items = CollectSelectableItems(node.Peer);
            if (childIndex < 0 || childIndex >= items.Count)
                return ValueTask.FromResult(false);

            items[childIndex].RemoveFromSelection();
            return ValueTask.FromResult(true);
        }

        private static List<ISelectionItemProvider> CollectSelectableItems(AutomationPeer peer)
        {
            var result = new List<ISelectionItemProvider>();
            CollectSelectableItemsCore(peer.GetChildren(), result);
            return result;
        }

        private static void CollectSelectableItemsCore(
            IReadOnlyList<AutomationPeer> children,
            List<ISelectionItemProvider> result)
        {
            foreach (var child in children)
            {
                if (child.GetProvider<ISelectionItemProvider>() is { } item)
                    result.Add(item);
                else
                    CollectSelectableItemsCore(child.GetChildren(), result);
            }
        }
    }
}
