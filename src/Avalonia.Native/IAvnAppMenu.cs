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
        private CompositeDisposable _propertyDisposables = new CompositeDisposable();

        internal NativeMenu ManagedMenu { get; private set; }

        private void RemoveAndDispose(IAvnAppMenuItem item)
        {
            _menuItemLookup.Remove(item.ManagedMenuItem);
            _menuItems.Remove(item);
            RemoveItem(item);

            item.Deinitialise();
            item.Dispose();
        }

        private void MoveExistingTo(int index, IAvnAppMenuItem item)
        {
            _menuItems.Remove(item);
            _menuItems.Insert(index, item);

            RemoveItem(item);
            InsertItem(index, item);
        }

        private IAvnAppMenuItem CreateNewAt(IAvaloniaNativeFactory factory, int index, NativeMenuItemBase item)
        {
            var result = CreateNew(factory, item);

            result.Initialise(item);

            _menuItemLookup.Add(result.ManagedMenuItem, result);
            _menuItems.Insert(index, result);

            InsertItem(index, result);

            return result;
        }

        private IAvnAppMenuItem CreateNew(IAvaloniaNativeFactory factory, NativeMenuItemBase item)
        {
            var nativeItem = item is NativeMenuItemSeperator ? factory.CreateMenuItemSeperator() : factory.CreateMenuItem();
            nativeItem.ManagedMenuItem = item;

            return nativeItem;
        }

        internal void Initialise(AvaloniaNativeMenuExporter exporter, NativeMenu managedMenu, string title)
        {
            _exporter = exporter;
            ManagedMenu = managedMenu;

            ((INotifyCollectionChanged)ManagedMenu.Items).CollectionChanged += OnMenuItemsChanged;

            if (!string.IsNullOrWhiteSpace(title))
            {
                using (var buffer = new Utf8Buffer(title))
                {
                    Title = buffer.DangerousGetHandle();
                }
            }
        }

        internal void Deinitialise()
        {
            ((INotifyCollectionChanged)ManagedMenu.Items).CollectionChanged -= OnMenuItemsChanged;

            foreach (var item in _menuItems)
            {
                item.Deinitialise();
                item.Dispose();
            }
        }

        internal void Update(IAvaloniaNativeFactory factory, NativeMenu menu)
        {
            if (menu != ManagedMenu)
            {
                throw new ArgumentException("The menu being updated does not match.", nameof(menu));
            }

            for (int i = 0; i < menu.Items.Count; i++)
            {
                IAvnAppMenuItem nativeItem;

                if (i >= _menuItems.Count)
                {
                    nativeItem = CreateNewAt(factory, i, menu.Items[i]);
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
                    nativeItem = CreateNewAt(factory, i, menu.Items[i]);
                }

                if (menu.Items[i] is NativeMenuItem nmi)
                {
                    nativeItem.Update(_exporter, factory, nmi);
                }
            }

            while (_menuItems.Count > menu.Items.Count)
            {
                RemoveAndDispose(_menuItems[_menuItems.Count - 1]);
            }
        }

        private void OnMenuItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _exporter.QueueReset();
        }
    }
}
