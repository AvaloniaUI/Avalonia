// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Perspex.Collections;
using Perspex.Controls.Generators;
using Perspex.Input;
using Perspex.Interactivity;
using Perspex.Styling;
using Perspex.VisualTree;

namespace Perspex.Controls.Primitives
{
    /// <summary>
    /// An <see cref="ItemsControl"/> that maintains a selection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="SelectingItemsControl"/> provides a base class for <see cref="ItemsControl"/>s
    /// that maintain a selection (single or multiple). By default only its 
    /// <see cref="SelectedIndex"/> and <see cref="SelectedItem"/> properties are visible; the
    /// current multiple selection <see cref="SelectedItems"/> together with the 
    /// <see cref="SelectionMode"/> properties are protected, however a derived  class can expose 
    /// these if it wishes to support multiple selection.
    /// </para>
    /// <para>
    /// <see cref="SelectingItemsControl"/> maintains a selection respecting the current 
    /// <see cref="SelectionMode"/> but it does not react to user input; this must be handled in a
    /// derived class. It does, however, respond to <see cref="IsSelectedChangedEvent"/> events
    /// from items and updates the selection accordingly.
    /// </para>
    /// </remarks>
    public class SelectingItemsControl : ItemsControl
    {
        /// <summary>
        /// Defines the <see cref="SelectedIndex"/> property.
        /// </summary>
        public static readonly PerspexProperty<int> SelectedIndexProperty =
            PerspexProperty.RegisterDirect<SelectingItemsControl, int>(
                nameof(SelectedIndex),
                o => o.SelectedIndex,
                (o, v) => o.SelectedIndex = v);

        /// <summary>
        /// Defines the <see cref="SelectedItem"/> property.
        /// </summary>
        public static readonly PerspexProperty<object> SelectedItemProperty =
            PerspexProperty.RegisterDirect<SelectingItemsControl, object>(
                nameof(SelectedItem),
                o => o.SelectedItem,
                (o, v) => o.SelectedItem = v);

        /// <summary>
        /// Defines the <see cref="SelectedItems"/> property.
        /// </summary>
        protected static readonly PerspexProperty<IList> SelectedItemsProperty =
            PerspexProperty.RegisterDirect<SelectingItemsControl, IList>(
                nameof(SelectedItems),
                o => o.SelectedItems,
                (o, v) => o.SelectedItems = v);

        /// <summary>
        /// Defines the <see cref="SelectionMode"/> property.
        /// </summary>
        protected static readonly PerspexProperty<SelectionMode> SelectionModeProperty =
            PerspexProperty.Register<SelectingItemsControl, SelectionMode>(
                nameof(SelectionMode));

        /// <summary>
        /// Event that should be raised by items that implement <see cref="ISelectable"/> to
        /// notify the parent <see cref="SelectingItemsControl"/> that their selection state
        /// has changed.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> IsSelectedChangedEvent =
            RoutedEvent.Register<SelectingItemsControl, RoutedEventArgs>("IsSelectedChanged", RoutingStrategies.Bubble);

        private int _selectedIndex = -1;
        private object _selectedItem;
        private IList _selectedItems;
        private bool _ignoreContainerSelectionChanged;
        private bool _syncingSelectedItems;
        private IList _clearSelectedItemsAfterDataContextChanged;

        /// <summary>
        /// Initializes static members of the <see cref="SelectingItemsControl"/> class.
        /// </summary>
        static SelectingItemsControl()
        {
            IsSelectedChangedEvent.AddClassHandler<SelectingItemsControl>(x => x.ContainerSelectionChanged);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectingItemsControl"/> class.
        /// </summary>
        public SelectingItemsControl()
        {
            ItemContainerGenerator.ContainersInitialized.Subscribe(ContainersInitialized);
        }

        /// <summary>
        /// Gets or sets the index of the selected item.
        /// </summary>
        public int SelectedIndex
        {
            get
            {
                return _selectedIndex;
            }

            set
            {
                var old = SelectedIndex;
                var effective = (value >= 0 && value < Items?.Cast<object>().Count()) ? value : -1;

                if (old != effective)
                {
                    _selectedIndex = effective;
                    RaisePropertyChanged(SelectedIndexProperty, old, effective, BindingPriority.LocalValue);
                    SelectedItem = ElementAt(Items, effective);
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected item.
        /// </summary>
        public object SelectedItem
        {
            get
            {
                return _selectedItem;
            }

            set
            {
                var old = SelectedItem;
                var index = IndexOf(Items, value);
                var effective = index != -1 ? value : null;

                if (!object.Equals(effective, old))
                {
                    _selectedItem = effective;
                    RaisePropertyChanged(SelectedItemProperty, old, effective, BindingPriority.LocalValue);
                    SelectedIndex = index;

                    if (effective != null)
                    {
                        if (SelectedItems.Count != 1 || SelectedItems[0] != effective)
                        {
                            _syncingSelectedItems = true;
                            SelectedItems.Clear();
                            SelectedItems.Add(effective);
                            _syncingSelectedItems = false;
                        }
                    }
                    else if (SelectedItems.Count > 0)
                    {
                        if (!IsDataContextChanging)
                        {
                            SelectedItems.Clear();
                        }
                        else
                        {
                            // The DataContext is changing, and it's quite possible that our 
                            // selection is being cleared because both Items and SelectedItems
                            // are bound to something on the DataContext. However, if we clear
                            // the collection now, we may be clearing a the SelectedItems from
                            // the DataContext which is being unbound, so do it after DataContext
                            // has notified all interested parties, in 
                            // the OnDataContextFinishedChanging method.
                            _clearSelectedItemsAfterDataContextChanged = SelectedItems;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the selected items.
        /// </summary>
        protected IList SelectedItems
        {
            get
            {
                if (_selectedItems == null)
                {
                    _selectedItems = new PerspexList<object>();
                    SubscribeToSelectedItems();
                }

                return _selectedItems;
            }

            set
            {
                UnsubscribeFromSelectedItems();
                _selectedItems = value ?? new PerspexList<object>();
                SubscribeToSelectedItems();
            }
        }

        /// <summary>
        /// Gets or sets the selection mode.
        /// </summary>
        protected SelectionMode SelectionMode
        {
            get { return GetValue(SelectionModeProperty); }
            set { SetValue(SelectionModeProperty, value); }
        }

        /// <summary>
        /// Gets a value indicating whether <see cref="SelectionMode.AlwaysSelected"/> is set.
        /// </summary>
        protected bool AlwaysSelected => (SelectionMode & SelectionMode.AlwaysSelected) != 0;

        /// <summary>
        /// Tries to get the container that was the source of an event.
        /// </summary>
        /// <param name="eventSource">The control that raised the event.</param>
        /// <returns>The container or null if the event did not originate in a container.</returns>
        protected IControl GetContainerFromEventSource(IInteractive eventSource)
        {
            var item = ((IVisual)eventSource).GetSelfAndVisualAncestors()
                .OfType<ILogical>()
                .FirstOrDefault(x => x.LogicalParent == this);

            return item as IControl;
        }

        /// <inheritdoc/>
        protected override void ItemsChanged(PerspexPropertyChangedEventArgs e)
        {
            base.ItemsChanged(e);

            if (SelectedIndex != -1)
            {
                SelectedIndex = IndexOf((IEnumerable)e.NewValue, SelectedItem);
            }
            else if (AlwaysSelected && Items != null && Items.Cast<object>().Any())
            {
                SelectedIndex = 0;
            }
        }

        /// <inheritdoc/>
        protected override void ItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            base.ItemsCollectionChanged(sender, e);

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (AlwaysSelected && SelectedIndex == -1)
                    {
                        SelectedIndex = 0;
                    }

                    break;

                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Replace:
                    var selectedIndex = SelectedIndex;

                    if (selectedIndex >= e.OldStartingIndex &&
                        selectedIndex < e.OldStartingIndex + e.OldItems.Count)
                    {
                        if (!AlwaysSelected)
                        {
                            SelectedIndex = -1;
                        }
                        else
                        {
                            LostSelection();
                        }
                    }

                    break;

                case NotifyCollectionChangedAction.Reset:
                    SelectedIndex = IndexOf(e.NewItems, SelectedItem);
                    break;
            }
        }

        /// <inheritdoc/>
        protected override void OnDataContextFinishedChanging()
        {
            if (_clearSelectedItemsAfterDataContextChanged == SelectedItems)
            {
                _clearSelectedItemsAfterDataContextChanged.Clear();
            }

            _clearSelectedItemsAfterDataContextChanged = null;
        }

        /// <summary>
        /// Updates the selection for an item based on user interaction.
        /// </summary>
        /// <param name="index">The index of the item.</param>
        /// <param name="select">Whether the item should be selected or unselected.</param>
        /// <param name="rangeModifier">Whether the range modifier is enabled (i.e. shift key).</param>
        /// <param name="toggleModifier">Whether the toggle modifier is enabled (i.e. ctrl key).</param>
        protected void UpdateSelection(
            int index,
            bool select = true,
            bool rangeModifier = false,
            bool toggleModifier = false)
        {
            if (index != -1)
            {
                if (select)
                {
                    var mode = SelectionMode;
                    var toggle = toggleModifier || (mode & SelectionMode.Toggle) != 0;
                    var multi = (mode & SelectionMode.Multiple) != 0;
                    var range = multi && SelectedIndex != -1 && rangeModifier;

                    if (!toggle && !range)
                    {
                        SelectedIndex = index;
                    }
                    else if (multi && range)
                    {
                        SynchronizeItems(
                            SelectedItems, 
                            GetRange(Items, SelectedIndex, index));
                    }
                    else
                    {
                        var item = ElementAt(Items, index);
                        var i = SelectedItems.IndexOf(item);

                        if (i != -1 && (!AlwaysSelected || SelectedItems.Count > 1))
                        {
                            SelectedItems.Remove(item);
                        }
                        else
                        {
                            if (multi)
                            {
                                SelectedItems.Add(item);
                            }
                            else
                            {
                                SelectedIndex = index;
                            }
                        }
                    }

                    if (Presenter?.Panel != null)
                    {
                        var container = ItemContainerGenerator.ContainerFromIndex(index);
                        KeyboardNavigation.SetTabOnceActiveElement(
                            (InputElement)Presenter.Panel,
                            container);
                    }
                }
                else
                {
                    LostSelection();
                }
            }
        }

        /// <summary>
        /// Updates the selection for a container based on user interaction.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="select">Whether the container should be selected or unselected.</param>
        /// <param name="rangeModifier">Whether the range modifier is enabled (i.e. shift key).</param>
        /// <param name="toggleModifier">Whether the toggle modifier is enabled (i.e. ctrl key).</param>
        protected void UpdateSelection(
            IControl container,
            bool select = true,
            bool rangeModifier = false,
            bool toggleModifier = false)
        {
            var index = ItemContainerGenerator.IndexFromContainer(container);

            if (index != -1)
            {
                UpdateSelection(index, select, rangeModifier, toggleModifier);
            }
        }

        /// <summary>
        /// Updates the selection based on an event that may have originated in a container that 
        /// belongs to the control.
        /// </summary>
        /// <param name="eventSource">The control that raised the event.</param>
        /// <param name="select">Whether the container should be selected or unselected.</param>
        /// <param name="rangeModifier">Whether the range modifier is enabled (i.e. shift key).</param>
        /// <param name="toggleModifier">Whether the toggle modifier is enabled (i.e. ctrl key).</param>
        /// <returns>
        /// True if the event originated from a container that belongs to the control; otherwise
        /// false.
        /// </returns>
        protected bool UpdateSelectionFromEventSource(
            IInteractive eventSource, 
            bool select = true,
            bool rangeModifier = false,
            bool toggleModifier = false)
        {
            var container = GetContainerFromEventSource(eventSource);

            if (container != null)
            {
                UpdateSelection(container, select, rangeModifier, toggleModifier);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the item at the specified index in a collection.
        /// </summary>
        /// <param name="items">The collection.</param>
        /// <param name="index">The index.</param>
        /// <returns>The index of the item or -1 if the item was not found.</returns>
        private static object ElementAt(IEnumerable items, int index)
        {
            var typedItems = items?.Cast<object>();

            if (index != -1 && typedItems != null && index < typedItems.Count())
            {
                return typedItems.ElementAt(index) ?? null;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the index of an item in a collection.
        /// </summary>
        /// <param name="items">The collection.</param>
        /// <param name="item">The item.</param>
        /// <returns>The index of the item or -1 if the item was not found.</returns>
        private static int IndexOf(IEnumerable items, object item)
        {
            if (items != null && item != null)
            {
                var list = items as IList;

                if (list != null)
                {
                    return list.IndexOf(item);
                }
                else
                {
                    int index = 0;

                    foreach (var i in items)
                    {
                        if (Equals(i, item))
                        {
                            return index;
                        }

                        ++index;
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// Gets a range of items from an IEnumerable.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="first">The index of the first item.</param>
        /// <param name="last">The index of the last item.</param>
        /// <returns>The items.</returns>
        private static IEnumerable<object> GetRange(IEnumerable items, int first, int last)
        {
            var list = (items as IList) ?? items.Cast<object>().ToList();
            int step = first > last ? -1 : 1;

            for (int i = first; i != last; i += step)
            {
                yield return list[i];
            }

            yield return list[last];
        }

        /// <summary>
        /// Makes a list of objects equal another.
        /// </summary>
        /// <param name="items">The items collection.</param>
        /// <param name="desired">The desired items.</param>
        private static void SynchronizeItems(IList items, IEnumerable<object> desired)
        {
            int index = 0;

            foreach (var i in desired)
            {
                if (index < items.Count)
                {
                    if (items[index] != i)
                    {
                        items[index] = i;
                    }
                }
                else
                {
                    items.Add(i);
                }

                ++index;
            }

            while (index < items.Count)
            {
                items.RemoveAt(items.Count - 1);
            }
        }

        /// <summary>
        /// Called when new containers are initialized by the <see cref="ItemContainerGenerator"/>.
        /// </summary>
        /// <param name="containers">The containers.</param>
        private void ContainersInitialized(ItemContainers containers)
        {
            var selectedIndex = SelectedIndex;
            var selectedContainer = containers.Items.OfType<ISelectable>().FirstOrDefault(x => x.IsSelected);

            if (selectedContainer != null)
            {
                SelectedIndex = containers.Items.IndexOf((IControl)selectedContainer) + containers.StartingIndex;
            }
            else if (selectedIndex >= containers.StartingIndex &&
                     selectedIndex < containers.StartingIndex + containers.Items.Count)
            {
                var container = containers.Items[selectedIndex - containers.StartingIndex];
                MarkContainerSelected(container, true);
            }
        }

        /// <summary>
        /// Called when a container raises the <see cref="IsSelectedChangedEvent"/>.
        /// </summary>
        /// <param name="e">The event.</param>
        private void ContainerSelectionChanged(RoutedEventArgs e)
        {
            if (!_ignoreContainerSelectionChanged)
            {
                var selectable = (ISelectable)e.Source;

                if (selectable != null)
                {
                    UpdateSelectionFromEventSource(e.Source, selectable.IsSelected);
                }
            }
        }

        /// <summary>
        /// Called when the currently selected item is lost and the selection must be changed
        /// depending on the <see cref="SelectionMode"/> property.
        /// </summary>
        private void LostSelection()
        {
            var items = Items?.Cast<object>();

            if (items != null && AlwaysSelected)
            {
                var index = Math.Min(SelectedIndex, items.Count() - 1);

                if (index > -1)
                {
                    SelectedItem = items.ElementAt(index);
                    return;
                }
            }

            SelectedIndex = -1;
        }

        /// <summary>
        /// Sets a container's 'selected' class or <see cref="ISelectable.IsSelected"/>.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="selected">Whether the control is selected</param>
        private void MarkContainerSelected(IControl container, bool selected)
        {
            try
            {
                var selectable = container as ISelectable;
                var styleable = container as IStyleable;

                _ignoreContainerSelectionChanged = true;

                if (selectable != null)
                {
                    selectable.IsSelected = selected;
                }
                else if (styleable != null)
                {
                    if (selected)
                    {
                        styleable.Classes.Add(":selected");
                    }
                    else
                    {
                        styleable.Classes.Remove(":selected");
                    }
                }
            }
            finally
            {
                _ignoreContainerSelectionChanged = false;
            }
        }

        /// <summary>
        /// Sets an item container's 'selected' class or <see cref="ISelectable.IsSelected"/>.
        /// </summary>
        /// <param name="index">The index of the item.</param>
        /// <param name="selected">Whether the item should be selected or deselected.</param>
        private void MarkItemSelected(int index, bool selected)
        {
            var container = ItemContainerGenerator.ContainerFromIndex(index);

            if (container != null)
            {
                MarkContainerSelected(container, selected);
            }
        }

        /// <summary>
        /// Sets an item container's 'selected' class or <see cref="ISelectable.IsSelected"/>.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="selected">Whether the item should be selected or deselected.</param>
        private void MarkItemSelected(object item, bool selected)
        {
            var index = IndexOf(Items, item);

            if (index != -1)
            {
                MarkItemSelected(index, selected);
            }
        }

        /// <summary>
        /// Called when the <see cref="SelectedItems"/> CollectionChanged event is raised.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void SelectedItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    SelectedItemsAdded(e.NewItems.Cast<object>().ToList());
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (SelectedItems.Count == 0)
                    {
                        if (!_syncingSelectedItems)
                        {
                            SelectedIndex = -1;
                        }
                    }
                    else
                    {
                        foreach (var item in e.OldItems)
                        {
                            MarkItemSelected(item, false);
                        }
                    }

                    break;

                case NotifyCollectionChangedAction.Reset:
                    foreach (var item in ItemContainerGenerator.Containers)
                    {
                        MarkContainerSelected(item, false);
                    }

                    if (!_syncingSelectedItems)
                    {
                        SelectedIndex = -1;
                    }

                    SelectedItemsAdded(SelectedItems);
                    break;

                case NotifyCollectionChangedAction.Replace:
                    foreach (var item in e.OldItems)
                    {
                        MarkItemSelected(item, false);
                    }

                    foreach (var item in e.NewItems)
                    {
                        MarkItemSelected(item, true);
                    }

                    if (SelectedItem != SelectedItems[0] && !_syncingSelectedItems)
                    {
                        var oldItem = SelectedItem;
                        var oldIndex = SelectedIndex;
                        var item = SelectedItems[0];
                        var index = IndexOf(Items, item);
                        _selectedIndex = index;
                        _selectedItem = item;
                        RaisePropertyChanged(SelectedIndexProperty, oldIndex, index, BindingPriority.LocalValue);
                        RaisePropertyChanged(SelectedItemProperty, oldItem, item, BindingPriority.LocalValue);
                    }

                    break;
            }
        }

        /// <summary>
        /// Called when items are added to the <see cref="SelectedItems"/> collection.
        /// </summary>
        /// <param name="items">The added items.</param>
        private void SelectedItemsAdded(IList items)
        {
            if (items.Count > 0)
            {
                foreach (var item in items)
                {
                    MarkItemSelected(item, true);
                }

                if (SelectedItem == null && !_syncingSelectedItems)
                {
                    var index = IndexOf(Items, items[0]);

                    if (index != -1)
                    {
                        _selectedItem = items[0];
                        _selectedIndex = index;
                        RaisePropertyChanged(SelectedIndexProperty, -1, index, BindingPriority.LocalValue);
                        RaisePropertyChanged(SelectedItemProperty, null, items[0], BindingPriority.LocalValue);
                    }
                }
            }
        }

        /// <summary>
        /// Subscribes to the <see cref="SelectedItems"/> CollectionChanged event, if any.
        /// </summary>
        private void SubscribeToSelectedItems()
        {
            var incc = _selectedItems as INotifyCollectionChanged;

            if (incc != null)
            {
                incc.CollectionChanged += SelectedItemsCollectionChanged;
            }

            SelectedItemsCollectionChanged(
                _selectedItems,
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        /// Unsubscribes from the <see cref="SelectedItems"/> CollectionChanged event, if any.
        /// </summary>
        private void UnsubscribeFromSelectedItems()
        {
            var incc = _selectedItems as INotifyCollectionChanged;

            if (incc != null)
            {
                incc.CollectionChanged -= SelectedItemsCollectionChanged;
            }
        }
    }
}
