using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Platform.Interop;

namespace Avalonia.Native.Interop
{
    public partial class IAvnAppMenu
    {
        private AvaloniaNativeMenuExporter _exporter;
        private List<IAvnAppMenuItem> _menuItems = new List<IAvnAppMenuItem>();
        private Dictionary<NativeMenuItemBase, IAvnAppMenuItem> _menuItemLookup = new Dictionary<NativeMenuItemBase, IAvnAppMenuItem>();

        internal NativeMenu ManagedMenu { get; private set; }

        private void RemoveAndDispose(IAvnAppMenuItem item)
        {
            _menuItemLookup.Remove(item.ManagedMenuItem);
            _menuItems.Remove(item);

            item.Update(null, null, null);
            RemoveItem(item);

            item.Dispose();
        }

        private void MoveExistingTo(int index, IAvnAppMenuItem item)
        {
            _menuItems.Remove(item);
            _menuItems.Insert(index, item);

            RemoveItem(item);
            InsertItem(index, item);
        }

        private void InsertNewAt(int index, IAvnAppMenuItem item)
        {
            _menuItemLookup.Add(item.ManagedMenuItem, item);
            _menuItems.Insert(index, item);

            InsertItem(index, item);
        }

        private IAvnAppMenuItem CreateNew(IAvaloniaNativeFactory factory, NativeMenuItemBase item)
        {
            var nativeItem = item is NativeMenuItemSeperator ? factory.CreateMenuItemSeperator() : factory.CreateMenuItem();
            nativeItem.ManagedMenuItem = item;

            return nativeItem;
        }

        internal IDisposable Update(AvaloniaNativeMenuExporter exporter, IAvaloniaNativeFactory factory, NativeMenu menu, string title = "")
        {
            var disposables = new CompositeDisposable();

            if (ManagedMenu == null)
            {
                ManagedMenu = menu;
            }
            else if (ManagedMenu != menu)
            {
                ManagedMenu = menu;
            }

            _exporter = exporter;

            ((INotifyCollectionChanged)ManagedMenu.Items).CollectionChanged += OnMenuItemsChanged;

            disposables.Add(Disposable.Create(() => ((INotifyCollectionChanged)ManagedMenu.Items).CollectionChanged -= OnMenuItemsChanged));

            if (!string.IsNullOrWhiteSpace(title))
            {
                using (var buffer = new Utf8Buffer(title))
                {
                    Title = buffer.DangerousGetHandle();
                }
            }

            for (int i = 0; i < menu.Items.Count; i++)
            {
                IAvnAppMenuItem nativeItem = null;

                if (i >= _menuItems.Count)
                {
                    nativeItem = CreateNew(factory, menu.Items[i]);

                    InsertNewAt(i, nativeItem);
                }
                else if (menu.Items[i] == _menuItems[i].ManagedMenuItem)
                {
                    nativeItem = _menuItems[i];
                }
                else if (_menuItemLookup.TryGetValue(menu.Items[i], out nativeItem))
                {
                    MoveExistingTo(i, nativeItem);
                }
                else
                {
                    nativeItem = CreateNew(factory, menu.Items[i]);

                    InsertNewAt(i, nativeItem);
                }

                if (menu.Items[i] is NativeMenuItem nmi)
                {
                    disposables.Add(nativeItem.Update(exporter, factory, nmi));
                }
            }

            while (_menuItems.Count > menu.Items.Count)
            {
                RemoveAndDispose(_menuItems[_menuItems.Count - 1]);
            }

            return disposables;
        }

        private void OnMenuItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _exporter.QueueReset();
        }
    }
}
