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
        private NativeMenu _menu;
        private List<IAvnAppMenuItem> _menuItems = new List<IAvnAppMenuItem>();
        private Dictionary<NativeMenuItemBase, IAvnAppMenuItem> _menuItemLookup = new Dictionary<NativeMenuItemBase, IAvnAppMenuItem>();

        private void Remove(IAvnAppMenuItem item)
        {
            _menuItemLookup.Remove(item.Managed);
            _menuItems.Remove(item);            

            RemoveItem(item);
        }

        private void InsertAt(int index, IAvnAppMenuItem item)
        {
            if (item.Managed == null)
            {
                throw new InvalidOperationException("Cannot insert item that with Managed link null");
            }

            _menuItemLookup.Add(item.Managed, item);
            _menuItems.Insert(index, item);

            InsertItem(index, item); // todo change to insertatimpl
        }

        private IAvnAppMenuItem CreateNew(IAvaloniaNativeFactory factory, NativeMenuItemBase item)
        {
            var nativeItem = item is NativeMenuItemSeperator ? factory.CreateMenuItemSeperator() : factory.CreateMenuItem();
            nativeItem.Managed = item;

            return nativeItem;
        }

        internal IDisposable Update(AvaloniaNativeMenuExporter exporter, IAvaloniaNativeFactory factory, NativeMenu menu, string title = "")
        {
            var disposables = new CompositeDisposable();

            if (_menu == null)
            {
                _menu = menu;
            }
            else if (_menu != menu)
            {                
                _menu = menu;
            }

            _exporter = exporter;

            ((INotifyCollectionChanged)_menu.Items).CollectionChanged += IAvnAppMenu_CollectionChanged;

            disposables.Add(Disposable.Create(() => ((INotifyCollectionChanged)_menu.Items).CollectionChanged -= IAvnAppMenu_CollectionChanged));

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

                if (i >= _menuItems.Count || menu.Items[i] != _menuItems[i].Managed)
                {
                    if (_menuItemLookup.TryGetValue(menu.Items[i], out nativeItem))
                    {
                        Remove(nativeItem);
                    }
                    else
                    {
                        nativeItem = CreateNew(factory, menu.Items[i]);
                    }
                }
                else
                {
                    nativeItem = _menuItems[i];
                    Remove(nativeItem);
                }

                if (menu.Items[i] is NativeMenuItem nmi)
                {
                    disposables.Add(nativeItem.Update(exporter, factory, nmi));
                }
                        
                InsertAt(i, nativeItem);
            }

            for (int i = menu.Items.Count; i < _menuItems.Count; i++)
            {
                Remove(_menuItems[i]);
            }

            return disposables;
        }

        private void IAvnAppMenu_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _exporter.QueueReset();
        }
    }
}
