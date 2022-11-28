using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
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
        /// Scrolls the specified item into view.
        /// </summary>
        /// <param name="index">The index of the item.</param>
        protected internal abstract void ScrollIntoView(int index);

        /// <summary>
        /// Returns the container for the item at the specified index.
        /// </summary>
        /// <param name="index">The index of the item to retrieve.</param>
        /// <returns>
        /// The container for the item at the specified index within the item collection, if the
        /// item is realized; otherwise, null.
        /// </returns>
        protected internal abstract Control? ContainerFromIndex(int index);

        /// <summary>
        /// Returns the index to the item that has the specified realized container.
        /// </summary>
        /// <param name="container">The generated container to retrieve the item index for.</param>
        /// <returns>
        /// The index to the item that corresponds to the specified realized container, or -1 if 
        /// <paramref name="container"/> is not found.
        /// </returns>
        protected internal abstract int IndexFromContainer(Control container);

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

        /// <summary>
        /// Adds the specified <see cref="Control"/> to the <see cref="Panel.Children"/> collection
        /// of a <see cref="VirtualizingPanel"/> element.
        /// </summary>
        /// <param name="control">The control to add to the collection.</param>
        protected void AddInternalChild(Control control)
        {
            var itemsControl = EnsureItemsControl();
            itemsControl.AddLogicalChild(control);
            Children.Add(control);
        }

        /// <summary>
        /// Adds the specified <see cref="Control"/> to the <see cref="Panel.Children"/> collection
        /// of a <see cref="VirtualizingPanel"/> element at the specified index position.
        /// </summary>
        /// <param name="index">
        /// The index position within the collection at which the child element is inserted.
        /// </param>
        /// <param name="control">The control to add to the collection.</param>
        protected void InsertInternalChild(int index, Control control)
        {
            var itemsControl = EnsureItemsControl();
            itemsControl.AddLogicalChild(control);
            Children.Insert(index, control);
        }

        /// <summary>
        /// Removes child elements from the <see cref="Panel.Children"/> collection.
        /// </summary>
        /// <param name="index">
        /// The beginning index position within the collection at which the first child element is
        /// removed.
        /// </param>
        /// <param name="count">The number of child elements to remove.</param>
        protected void RemoveInternalChildRange(int index, int count)
        {
            var itemsControl = EnsureItemsControl();
            
            for (var i = 0; i < count; ++i)
            {
                var c = Children[i];
                itemsControl.RemoveLogicalChild(c);
            }

            Children.RemoveRange(index, count);
        }

        internal void Attach(ItemsControl itemsControl)
        {
            if (ItemsControl is not null)
                throw new InvalidOperationException("The VirtualizingPanel is already attached to an ItemsControl");

            ItemsControl = itemsControl;
            ItemsControl.PropertyChanged += OnItemsControlPropertyChanged;

            if (ItemsControl.Items is INotifyCollectionChanged incc)
                incc.CollectionChanged += OnItemsControlItemsChanged;
        }

        internal void Detach()
        {
            var itemsControl = EnsureItemsControl();

            itemsControl.PropertyChanged -= OnItemsControlPropertyChanged;

            if (itemsControl.Items is INotifyCollectionChanged incc)
                incc.CollectionChanged -= OnItemsControlItemsChanged;

            ItemsControl = null;
            Children.Clear();
        }

        private ItemsControl EnsureItemsControl()
        {
            if (ItemsControl is null)
                ThrowNotAttached();
            return ItemsControl;
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

        [DoesNotReturn]
        private static void ThrowNotAttached()
        {
            throw new InvalidOperationException("The VirtualizingPanel does not belong to an ItemsControl.");
        }
    }
}
