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
        private NativeMenuItem? _parent;

        [Content]
        public IList<NativeMenuItemBase> Items => _items;

        /// <summary>
        /// Raised when the menu requests an update.
        /// </summary>
        /// <remarks>
        /// Use this event to add, remove or modify menu items before a menu is
        /// shown or a hotkey is pressed.
        /// </remarks>
        public event EventHandler<EventArgs>? NeedsUpdate;

        /// <summary>
        /// Raised before the menu is opened.
        /// </summary>
        /// <remarks>
        /// Do not update the menu in this event; use <see cref="NeedsUpdate"/>.
        /// </remarks>
        public event EventHandler<EventArgs>? Opening;
        
        /// <summary>
        /// Raised after the menu is closed.
        /// </summary>
        /// <remarks>
        /// Do not update the menu in this event; use <see cref="NeedsUpdate"/>.
        /// </remarks>
        public event EventHandler<EventArgs>? Closed;

        public NativeMenu()
        {
            _items.Validate = Validator;
            _items.CollectionChanged += ItemsChanged;
        }

        void INativeMenuExporterEventsImplBridge.RaiseNeedsUpdate()
        {
            NeedsUpdate?.Invoke(this, EventArgs.Empty);
        }

        void INativeMenuExporterEventsImplBridge.RaiseOpening()
        {
            Opening?.Invoke(this, EventArgs.Empty);
        }

        void INativeMenuExporterEventsImplBridge.RaiseClosed()
        {
            Closed?.Invoke(this, EventArgs.Empty);
        }

        private void Validator(NativeMenuItemBase obj)
        {
            if (obj.Parent != null)
                throw new InvalidOperationException("NativeMenuItem already has a parent");
        }

        private void ItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
                foreach (NativeMenuItemBase i in e.OldItems)
                    i.Parent = null;
            if (e.NewItems != null)
                foreach (NativeMenuItemBase i in e.NewItems)
                    i.Parent = this;
        }

        public static readonly DirectProperty<NativeMenu, NativeMenuItem?> ParentProperty =
            AvaloniaProperty.RegisterDirect<NativeMenu, NativeMenuItem?>(nameof(Parent), o => o.Parent);

        public NativeMenuItem? Parent
        {
            get => _parent;
            internal set => SetAndRaise(ParentProperty, ref _parent, value);
        }

        public void Add(NativeMenuItemBase item) => _items.Add(item);

        public IEnumerator<NativeMenuItemBase> GetEnumerator() => _items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
