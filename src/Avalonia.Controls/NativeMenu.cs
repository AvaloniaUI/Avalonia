using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.Collections;
using Avalonia.Metadata;

namespace Avalonia.Controls
{
    public partial class NativeMenu : AvaloniaObject, IEnumerable<NativeMenuItemBase>, INativeMenuExporterEventsImplBridge
    {
        private readonly AvaloniaList<NativeMenuItemBase> _items =
            new AvaloniaList<NativeMenuItemBase> { ResetBehavior = ResetBehavior.Remove };
        private NativeMenuItem _parent;
        [Content]
        public IList<NativeMenuItemBase> Items => _items;

        /// <summary>
        /// Raised when the user clicks the menu and before its opened. Use this event to update the menu dynamically.
        /// </summary>
        public event EventHandler<EventArgs> Opening;

        public NativeMenu()
        {
            _items.Validate = Validator;
            _items.CollectionChanged += ItemsChanged;
        }

        void INativeMenuExporterEventsImplBridge.RaiseNeedsUpdate()
        {
            Opening?.Invoke(this, EventArgs.Empty);
        }

        private void Validator(NativeMenuItemBase obj)
        {
            if (obj.Parent != null)
                throw new InvalidOperationException("NativeMenuItem already has a parent");
        }

        private void ItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
                foreach (NativeMenuItemBase i in e.OldItems)
                    i.Parent = null;
            if (e.NewItems != null)
                foreach (NativeMenuItemBase i in e.NewItems)
                    i.Parent = this;
        }

        public static readonly DirectProperty<NativeMenu, NativeMenuItem> ParentProperty =
            AvaloniaProperty.RegisterDirect<NativeMenu, NativeMenuItem>("Parent", o => o.Parent, (o, v) => o.Parent = v);

        public NativeMenuItem Parent
        {
            get => _parent;
            set => SetAndRaise(ParentProperty, ref _parent, value);
        }

        public void Add(NativeMenuItemBase item) => _items.Add(item);

        public IEnumerator<NativeMenuItemBase> GetEnumerator() => _items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
