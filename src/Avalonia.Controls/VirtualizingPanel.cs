using System.Collections;
using System.Collections.Specialized;
using Avalonia.Controls.Utils;

namespace Avalonia.Controls
{
    /// <summary>
    /// Base class for panels that can be used to virtualize items.
    /// </summary>
    public abstract class VirtualizingPanel : Panel
    {
        private ItemsControl? _itemsControl;

        protected ItemsControl? ItemsControl 
        {
            get => _itemsControl;
            private set
            {
                if (_itemsControl != value)
                {
                    var oldValue = _itemsControl;
                    _itemsControl= value;
                    OnItemsControlChanged(oldValue);
                }
            }
        }

        /// <summary>
        /// Called when the <see cref="ItemsControl"/> that owns the panel changes.
        /// </summary>
        /// <param name="oldValue">
        /// The old value of the <see cref="ItemsControl"/> property.
        /// </param>
        protected virtual void OnItemsControlChanged(ItemsControl? oldValue)
        {
        }

        /// <summary>
        /// Called when the <see cref="ItemsControl.Items"/> collection of the owner
        /// <see cref="ItemsControl"/> changes.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="e">The event args.</param>
        /// <remarks>
        /// This method is called a <see cref="INotifyCollectionChanged"/> event is raised by
        /// the items, or when the <see cref="ItemsControl.Items"/> property is assigned a
        /// new collection, in which case the <see cref="NotifyCollectionChangedAction"/> will
        /// be <see cref="NotifyCollectionChangedAction.Reset"/>.
        /// </remarks>
        protected virtual void OnItemsChanged(IList items, NotifyCollectionChangedEventArgs e)
        {
        }

        internal void Attach(ItemsControl itemsControl)
        {
            ItemsControl = itemsControl;
            ItemsControl.PropertyChanged += OnItemsControlPropertyChanged;

            if (ItemsControl.Items is INotifyCollectionChanged incc)
                incc.CollectionChanged += OnItemsControlItemsChanged;
        }

        internal void Detach()
        {
            if (ItemsControl is null)
                return;

            ItemsControl.PropertyChanged -= OnItemsControlPropertyChanged;

            if (ItemsControl.Items is INotifyCollectionChanged incc)
                incc.CollectionChanged -= OnItemsControlItemsChanged;

            ItemsControl = null;
            Children.Clear();
        }

        private protected virtual void OnItemsControlPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == ItemsControl.ItemsProperty)
            {
                if (e.OldValue is INotifyCollectionChanged inccOld)
                    inccOld.CollectionChanged -= OnItemsControlItemsChanged;
                OnItemsControlItemsChanged(null, CollectionUtils.ResetEventArgs);
                if (e.NewValue is INotifyCollectionChanged inccNew)
                    inccNew.CollectionChanged += OnItemsControlItemsChanged;
            }
        }

        private void OnItemsControlItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_itemsControl?.Items is IList items)
                OnItemsChanged(items, e);
        }
    }
}
