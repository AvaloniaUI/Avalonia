using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Threading;

namespace Avalonia.Native
{
    class AvaloniaNativeMenuExporter : ITopLevelNativeMenuExporter
    {
        private NativeMenu _menu;
        private bool _resetQueued;
        private Dictionary<int, NativeMenuItem> _idsToItems = new Dictionary<int, NativeMenuItem>();
        private Dictionary<NativeMenuItem, int> _itemsToIds = new Dictionary<NativeMenuItem, int>();
        private uint _revision = 1;

        public bool IsNativeMenuExported => throw new NotImplementedException();

        public event EventHandler OnIsNativeMenuExportedChanged;

        private event Action<(uint revision, int parent)> LayoutUpdated;

        public void SetNativeMenu(NativeMenu menu)
        {
            if (menu == null)
                menu = new NativeMenu();

            if (_menu != null)
                ((INotifyCollectionChanged)_menu.Items).CollectionChanged -= OnMenuItemsChanged;
            _menu = menu;
            ((INotifyCollectionChanged)_menu.Items).CollectionChanged += OnMenuItemsChanged;

            DoLayoutReset();
        }

        public void SetPrependApplicationMenu(bool prepend)
        {
            throw new NotImplementedException();
        }

        private void OnItemPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            QueueReset();
        }

        private void OnMenuItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            QueueReset();
        }

        /*
                 This is basic initial implementation, so we don't actually track anything and
                 just reset the whole layout on *ANY* change
                 
                 This is not how it should work and will prevent us from implementing various features,
                 but that's the fastest way to get things working, so...
             */
        void DoLayoutReset()
        {
            _resetQueued = false;
            foreach (var i in _idsToItems.Values)
            {
                i.PropertyChanged -= OnItemPropertyChanged;
                if (i.Menu != null)
                    ((INotifyCollectionChanged)i.Menu.Items).CollectionChanged -= OnMenuItemsChanged;
            }

            _idsToItems.Clear();
            _itemsToIds.Clear();

            _revision++;

            LayoutUpdated?.Invoke((_revision, 0));
        }

        private void QueueReset()
        {
            if (_resetQueued)
                return;
            _resetQueued = true;
            Dispatcher.UIThread.Post(DoLayoutReset, DispatcherPriority.Background);
        }
    }
}
