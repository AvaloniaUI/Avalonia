using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Platform.Interop;

namespace Avalonia.Native.Interop
{
    class MenuEvents : CallbackBase, IAvnMenuEvents
    {
        private IAvnMenu _parent;

        public void Initialise(IAvnMenu parent)
        {
            _parent = parent;
        }

        public void NeedsUpdate()
        {
            _parent?.RaiseNeedsUpdate();
        }
    }

    public partial class IAvnMenu
    {
        private MenuEvents _events;
        private AvaloniaNativeMenuExporter _exporter;
        private List<IAvnMenuItem> _menuItems = new List<IAvnMenuItem>();
        private Dictionary<NativeMenuItemBase, IAvnMenuItem> _menuItemLookup = new Dictionary<NativeMenuItemBase, IAvnMenuItem>();
        private CompositeDisposable _propertyDisposables = new CompositeDisposable();

        internal void RaiseNeedsUpdate()
        {
            (ManagedMenu as INativeMenuExporterEventsImplBridge).RaiseNeedsUpdate();

            _exporter.UpdateIfNeeded();
        }

        internal NativeMenu ManagedMenu { get; private set; }

        public static IAvnMenu Create(IAvaloniaNativeFactory factory)
        {
            var events = new MenuEvents();

            var menu = factory.CreateMenu(events);

            events.Initialise(menu);

            menu._events = events;

            return menu;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _events.Dispose();
            }
        }

        private void RemoveAndDispose(IAvnMenuItem item)
        {
            _menuItemLookup.Remove(item.ManagedMenuItem);
            _menuItems.Remove(item);
            RemoveItem(item);

            item.Deinitialise();
            item.Dispose();
        }

        private void MoveExistingTo(int index, IAvnMenuItem item)
        {
            _menuItems.Remove(item);
            _menuItems.Insert(index, item);

            RemoveItem(item);
            InsertItem(index, item);
        }

        private IAvnMenuItem CreateNewAt(IAvaloniaNativeFactory factory, int index, NativeMenuItemBase item)
        {
            var result = CreateNew(factory, item);

            result.Initialise(item);

            _menuItemLookup.Add(result.ManagedMenuItem, result);
            _menuItems.Insert(index, result);

            InsertItem(index, result);

            return result;
        }

        private IAvnMenuItem CreateNew(IAvaloniaNativeFactory factory, NativeMenuItemBase item)
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
                IAvnMenuItem nativeItem;

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
