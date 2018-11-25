// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls.Generators;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Primitives
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
        /// Defines the <see cref="AutoScrollToSelectedItem"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> AutoScrollToSelectedItemProperty =
            AvaloniaProperty.Register<SelectingItemsControl, bool>(
                nameof(AutoScrollToSelectedItem),
                defaultValue: true);

        /// <summary>
        /// Defines the <see cref="SelectedIndex"/> property.
        /// </summary>
        public static readonly DirectProperty<SelectingItemsControl, int> SelectedIndexProperty =
            AvaloniaProperty.RegisterDirect<SelectingItemsControl, int>(
                nameof(SelectedIndex),
                o => o.SelectedIndex,
                (o, v) => o.SelectedIndex = v,
                unsetValue: -1);

        /// <summary>
        /// Defines the <see cref="SelectedItem"/> property.
        /// </summary>
        public static readonly DirectProperty<SelectingItemsControl, object> SelectedItemProperty =
            AvaloniaProperty.RegisterDirect<SelectingItemsControl, object>(
                nameof(SelectedItem),
                o => o.SelectedItem,
                (o, v) => o.SelectedItem = v,
                defaultBindingMode: BindingMode.TwoWay);

        /// <summary>
        /// Defines the <see cref="SelectedItems"/> property.
        /// </summary>
        protected static readonly DirectProperty<SelectingItemsControl, IList> SelectedItemsProperty =
            AvaloniaProperty.RegisterDirect<SelectingItemsControl, IList>(
                nameof(SelectedItems),
                o => o.SelectedItems,
                (o, v) => o.SelectedItems = v);

        /// <summary>
        /// Defines the <see cref="SelectionMode"/> property.
        /// </summary>
        protected static readonly StyledProperty<SelectionMode> SelectionModeProperty =
            AvaloniaProperty.Register<SelectingItemsControl, SelectionMode>(
                nameof(SelectionMode));

        /// <summary>
        /// Event that should be raised by items that implement <see cref="ISelectable"/> to
        /// notify the parent <see cref="SelectingItemsControl"/> that their selection state
        /// has changed.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> IsSelectedChangedEvent =
            RoutedEvent.Register<SelectingItemsControl, RoutedEventArgs>(
                "IsSelectedChanged",
                RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="SelectionChanged"/> event.
        /// </summary>
        public static readonly RoutedEvent<SelectionChangedEventArgs> SelectionChangedEvent =
            RoutedEvent.Register<SelectingItemsControl, SelectionChangedEventArgs>(
                "SelectionChanged",
                RoutingStrategies.Bubble);

        private static readonly IList Empty = new object[0];

        private int _selectedIndex = -1;
        private object _selectedItem;
        private IList _selectedItems;
        private bool _ignoreContainerSelectionChanged;
        private bool _syncingSelectedItems;
        private int _updateCount;
        private int _updateSelectedIndex;
        private IList _updateSelectedItems;

        /// <summary>
        /// Initializes static members of the <see cref="SelectingItemsControl"/> class.
        /// </summary>
        static SelectingItemsControl()
        {
            IsSelectedChangedEvent.AddClassHandler<SelectingItemsControl>(x => x.ContainerSelectionChanged);
        }

        /// <summary>
        /// Occurs when the control's selection changes.
        /// </summary>
        public event EventHandler<SelectionChangedEventArgs> SelectionChanged
        {
            add { AddHandler(SelectionChangedEvent, value); }
            remove { RemoveHandler(SelectionChangedEvent, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to automatically scroll to newly selected items.
        /// </summary>
        public bool AutoScrollToSelectedItem
        {
            get { return GetValue(AutoScrollToSelectedItemProperty); }
            set { SetValue(AutoScrollToSelectedItemProperty, value); }
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
                if (_updateCount == 0)
                {
                    SetAndRaise(SelectedIndexProperty, ref _selectedIndex, (int val, ref int backing, Action<Action> notifyWrapper) =>
                    {
                        var old = backing;
                        var effective = (val >= 0 && val < Items?.Cast<object>().Count()) ? val : -1;

                        if (old != effective)
                        {
                            backing = effective;
                            notifyWrapper(() =>
                                RaisePropertyChanged(
                                    SelectedIndexProperty,
                                    old,
                                    effective,
                                    BindingPriority.LocalValue));
                            SelectedItem = ElementAt(Items, effective);
                        }
                    }, value);
                }
                else
                {
                    _updateSelectedIndex = value;
                    _updateSelectedItems = null;
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
                if (_updateCount == 0)
                {
                    SetAndRaise(SelectedItemProperty, ref _selectedItem, (object val, ref object backing, Action<Action> notifyWrapper) =>
                    {
                        var old = backing;
                        var index = IndexOf(Items, val);
                        var effective = index != -1 ? val : null;

                        if (!object.Equals(effective, old))
                        {
                            backing = effective;

                            notifyWrapper(() =>
                                RaisePropertyChanged(
                                    SelectedItemProperty,
                                    old,
                                    effective,
                                    BindingPriority.LocalValue));

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
                                SelectedItems.Clear();
                            }
                        }
                    }, value);
                }
                else
                {
                    _updateSelectedItems = new AvaloniaList<object>(value);
                    _updateSelectedIndex = int.MinValue;
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
                    _selectedItems = new AvaloniaList<object>();
                    SubscribeToSelectedItems();
                }

                return _selectedItems;
            }

            set
            {
                if (value?.IsFixedSize == true || value?.IsReadOnly == true)
                {
                    throw new NotSupportedException(
                        "Cannot use a fixed size or read-only collection as SelectedItems.");
                }

                UnsubscribeFromSelectedItems();
                _selectedItems = value ?? new AvaloniaList<object>();
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

        /// <inheritdoc/>
        public override void BeginInit()
        {
            base.BeginInit();
            ++_updateCount;
            _updateSelectedIndex = int.MinValue;
        }

        /// <inheritdoc/>
        public override void EndInit()
        {
            if (--_updateCount == 0)
            {
                UpdateFinished();
            }

            base.EndInit();
        }

        /// <summary>
        /// Scrolls the specified item into view.
        /// </summary>
        /// <param name="item">The item.</param>
        public void ScrollIntoView(object item) => Presenter?.ScrollIntoView(item);

        /// <summary>
        /// Tries to get the container that was the source of an event.
        /// </summary>
        /// <param name="eventSource">The control that raised the event.</param>
        /// <returns>The container or null if the event did not originate in a container.</returns>
        protected IControl GetContainerFromEventSource(IInteractive eventSource)
        {
            var item = ((IVisual)eventSource).GetSelfAndVisualAncestors()
                .OfType<IControl>()
                .FirstOrDefault(x => x.LogicalParent == this && ItemContainerGenerator?.IndexFromContainer(x) != -1);

            return item;
        }

        /// <inheritdoc/>
        protected override void ItemsChanged(AvaloniaPropertyChangedEventArgs e)
        {
            base.ItemsChanged(e);

            if (_updateCount == 0)
            {
                if (SelectedIndex != -1)
                {
                    SelectedIndex = IndexOf((IEnumerable)e.NewValue, SelectedItem);
                }
                else if (AlwaysSelected && Items != null && Items.Cast<object>().Any())
                {
                    SelectedIndex = 0;
                }
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
                            selectedIndex = SelectedIndex = -1;
                        }
                        else
                        {
                            LostSelection();
                        }
                    }

                    var items = Items?.Cast<object>();
                    if (selectedIndex >= items.Count())
                    {
                        selectedIndex = SelectedIndex = items.Count() - 1;
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    SelectedIndex = IndexOf(Items, SelectedItem);
                    break;
            }
        }

        /// <inheritdoc/>
        protected override void OnContainersMaterialized(ItemContainerEventArgs e)
        {
            base.OnContainersMaterialized(e);

            var selectedIndex = SelectedIndex;
            var selectedContainer = e.Containers
                .FirstOrDefault(x => (x.ContainerControl as ISelectable)?.IsSelected == true);

            if (selectedContainer != null)
            {
                SelectedIndex = selectedContainer.Index;
            }
            else if (selectedIndex >= e.StartingIndex &&
                     selectedIndex < e.StartingIndex + e.Containers.Count)
            {
                var container = e.Containers[selectedIndex - e.StartingIndex];

                if (container.ContainerControl != null)
                {
                    MarkContainerSelected(container.ContainerControl, true);
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnContainersDematerialized(ItemContainerEventArgs e)
        {
            base.OnContainersDematerialized(e);

            var panel = (InputElement)Presenter.Panel;

            if (panel != null)
            {
                foreach (var container in e.Containers)
                {
                    if (KeyboardNavigation.GetTabOnceActiveElement(panel) == container.ContainerControl)
                    {
                        KeyboardNavigation.SetTabOnceActiveElement(panel, null);
                        break;
                    }
                }
            }
        }

        protected override void OnContainersRecycled(ItemContainerEventArgs e)
        {
            foreach (var i in e.Containers)
            {
                if (i.ContainerControl != null && i.Item != null)
                {
                    var ms = MemberSelector;
                    bool selected = ms == null ? 
                        SelectedItems.Contains(i.Item) : 
                        SelectedItems.OfType<object>().Any(v => Equals(ms.Select(v), i.Item));

                    MarkContainerSelected(i.ContainerControl, selected);
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnDataContextBeginUpdate()
        {
            base.OnDataContextBeginUpdate();
            ++_updateCount;
        }

        /// <inheritdoc/>
        protected override void OnDataContextEndUpdate()
        {
            base.OnDataContextEndUpdate();

            if (--_updateCount == 0)
            {
                UpdateFinished();
            }
        }

        /// <summary>
        /// Moves the selection in the specified direction relative to the current selection.
        /// </summary>
        /// <param name="direction">The direction to move.</param>
        /// <param name="wrap">Whether to wrap when the selection reaches the first or last item.</param>
        /// <returns>True if the selection was moved; otherwise false.</returns>
        protected bool MoveSelection(NavigationDirection direction, bool wrap)
        {
            var from = SelectedIndex != -1 ? ItemContainerGenerator.ContainerFromIndex(SelectedIndex) : null;
            return MoveSelection(from, direction, wrap);
        }

        /// <summary>
        /// Moves the selection in the specified direction relative to the specified container.
        /// </summary>
        /// <param name="from">The container which serves as a starting point for the movement.</param>
        /// <param name="direction">The direction to move.</param>
        /// <param name="wrap">Whether to wrap when the selection reaches the first or last item.</param>
        /// <returns>True if the selection was moved; otherwise false.</returns>
        protected bool MoveSelection(IControl from, NavigationDirection direction, bool wrap)
        {
            if (Presenter?.Panel is INavigableContainer container &&
                GetNextControl(container, direction, from, wrap) is IControl next)
            {
                var index = ItemContainerGenerator.IndexFromContainer(next);

                if (index != -1)
                {
                    SelectedIndex = index;
                    return true;
                }
            }

            return false;
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
            var index = ItemContainerGenerator?.IndexFromContainer(container) ?? -1;

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
        /// Called when a container raises the <see cref="IsSelectedChangedEvent"/>.
        /// </summary>
        /// <param name="e">The event.</param>
        private void ContainerSelectionChanged(RoutedEventArgs e)
        {
            if (!_ignoreContainerSelectionChanged)
            {
                var control = e.Source as IControl;
                var selectable = e.Source as ISelectable;

                if (control != null &&
                    selectable != null &&
                    control.LogicalParent == this &&
                    ItemContainerGenerator?.IndexFromContainer(control) != -1)
                {
                    UpdateSelection(control, selectable.IsSelected);
                }
            }

            if (e.Source != this)
            {
                e.Handled = true;
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
        /// <returns>The previous selection state.</returns>
        private bool MarkContainerSelected(IControl container, bool selected)
        {
            try
            {
                var selectable = container as ISelectable;
                bool result;

                _ignoreContainerSelectionChanged = true;

                if (selectable != null)
                {
                    result = selectable.IsSelected;
                    selectable.IsSelected = selected;
                }
                else
                {
                    result = container.Classes.Contains(":selected");
                    ((IPseudoClasses)container.Classes).Set(":selected", selected);
                }

                return result;
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
            var container = ItemContainerGenerator?.ContainerFromIndex(index);

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
            var generator = ItemContainerGenerator;
            IList added = null;
            IList removed = null;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    SelectedItemsAdded(e.NewItems.Cast<object>().ToList());

                    if (AutoScrollToSelectedItem)
                    {
                        ScrollIntoView(e.NewItems[0]);
                    }

                    added = e.NewItems;
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (SelectedItems.Count == 0)
                    {
                        if (!_syncingSelectedItems)
                        {
                            SelectedIndex = -1;
                        }
                    }

                    foreach (var item in e.OldItems)
                    {
                        MarkItemSelected(item, false);
                    }

                    removed = e.OldItems;
                    break;

                case NotifyCollectionChangedAction.Reset:
                    if (generator != null)
                    {
                        removed = new List<object>();

                        foreach (var item in generator.Containers)
                        {
                            if (item?.ContainerControl != null)
                            {
                                if (MarkContainerSelected(item.ContainerControl, false))
                                {
                                    removed.Add(item.Item);
                                }
                            }
                        }
                    }

                    if (SelectedItems.Count > 0)
                    {
                        _selectedItem = null;
                        SelectedItemsAdded(SelectedItems);
                        added = SelectedItems;
                    }
                    else if (!_syncingSelectedItems)
                    {
                        SelectedIndex = -1;
                    }

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

                    added = e.NewItems;
                    removed = e.OldItems;
                    break;
            }

            if (added?.Count > 0 || removed?.Count > 0)
            {
                var changed = new SelectionChangedEventArgs(
                    SelectionChangedEvent,
                    added ?? Empty,
                    removed ?? Empty);
                RaiseEvent(changed);
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

        private void UpdateFinished()
        {
            if (_updateSelectedIndex != int.MinValue)
            {
                SelectedIndex = _updateSelectedIndex;
            }
            else if (_updateSelectedItems != null)
            {
                SelectedItems = _updateSelectedItems;
            }
        }
    }
}
