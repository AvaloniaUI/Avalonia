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
    /// multiple selection properties <see cref="SelectedIndexes"/> and <see cref="SelectedItems"/>
    /// together with the <see cref="SelectionMode"/> properties are protected, however a derived 
    /// class can expose these if it wishes to support multiple selection.
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
        /// Defines the <see cref="SelectedIndexes"/> property.
        /// </summary>
        protected static readonly PerspexProperty<IPerspexList<int>> SelectedIndexesProperty =
            PerspexProperty.RegisterDirect<SelectingItemsControl, IPerspexList<int>>(
                nameof(SelectedIndexes),
                o => o.SelectedIndexes);

        /// <summary>
        /// Defines the <see cref="SelectedItems"/> property.
        /// </summary>
        protected static readonly PerspexProperty<IPerspexList<object>> SelectedItemsProperty =
            PerspexProperty.RegisterDirect<SelectingItemsControl, IPerspexList<object>>(
                nameof(SelectedItems),
                o => o.SelectedItems);

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

        private PerspexList<int> _selectedIndexes = new PerspexList<int>();
        private PerspexList<object> _selectedItems = new PerspexList<object>();
        private bool _ignoreContainerSelectionChanged;

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
            _selectedIndexes.Validate = ValidateIndex;
            _selectedIndexes.ForEachItem(SelectedIndexesAdded, SelectedIndexesRemoved, SelectionReset);
            _selectedItems.ForEachItem(SelectedItemsAdded, SelectedItemsRemoved, SelectionReset);
        }

        /// <summary>
        /// Gets or sets the index of the selected item.
        /// </summary>
        public int SelectedIndex
        {
            get
            {
                return _selectedIndexes.Count > 0 ? _selectedIndexes[0]: -1;
            }

            set
            {
                var old = SelectedIndex;
                var effective = (value >= 0 && value < Items?.Cast<object>().Count()) ? value : -1;

                if (old != effective)
                {
                    _selectedIndexes.Clear();

                    if (effective != -1)
                    {
                        _selectedIndexes.Add(effective);
                    }

                    RaisePropertyChanged(SelectedIndexProperty, old, effective, BindingPriority.LocalValue);
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
                return _selectedItems.FirstOrDefault();
            }

            set
            {
                var old = SelectedItem;
                var effective = Items?.Cast<object>().Contains(value) == true ? value : null;

                if (effective != old)
                {
                    _selectedItems.Clear();

                    if (effective != null)
                    {
                        _selectedItems.Add(effective);
                    }

                    RaisePropertyChanged(SelectedItemProperty, old, effective, BindingPriority.LocalValue);
                }
            }
        }

        /// <summary>
        /// Gets the selected indexes.
        /// </summary>
        protected IPerspexList<int> SelectedIndexes
        {
            get { return _selectedIndexes; }
        }

        /// <summary>
        /// Gets the selected items.
        /// </summary>
        protected IPerspexList<object> SelectedItems
        {
            get { return _selectedItems; }
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
            else if (AlwaysSelected && Items != null & Items.Cast<object>().Any())
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
                    var range = multi && SelectedIndexes.Count > 0 ? rangeModifier : false;

                    if (!toggle && !range)
                    {
                        SelectedIndex = index;
                    }
                    else if (multi && range)
                    {
                        var first = SelectedIndexes[0];

                        // TODO: Don't deselect items in new selection.
                        SelectedIndexes.Clear();
                        SelectedIndexes.AddRange(Range(first, index));
                    }
                    else
                    {
                        var i = SelectedIndexes.IndexOf(index);

                        if (i != -1 && (!AlwaysSelected || SelectedItems.Count > 1))
                        {
                            SelectedIndexes.RemoveAt(i);
                        }
                        else
                        {
                            if (multi)
                            {
                                SelectedIndexes.Add(index);
                            }
                            else
                            {
                                SelectedIndex = index;
                            }
                        }
                    }
                }
                else
                {
                    LostSelection();
                }
            }
        }

        private IEnumerable<int> Range(int first, int last)
        {
            int step = first > last ? -1 : 1;

            for (int i = first; i != last; i += step)
            {
                yield return i;
            }

            yield return last;
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
        /// Updates the selection based on an event source that may have originated in a container
        /// that belongs to the control.
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
            var item = GetContainerFromEventSource(eventSource);

            if (item != null)
            {
                UpdateSelection(item, select, rangeModifier, toggleModifier);
                return true;
            }

            return false;
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
        /// Sets an item container's 'selected' class or <see cref="ISelectable.IsSelected"/>.
        /// </summary>
        /// <param name="index">The index of the item.</param>
        /// <param name="selected">Whether the control is selected</param>
        /// <returns>The container.</returns>
        private IControl MarkIndexSelected(int index, bool selected)
        {
            var container = ItemContainerGenerator.ContainerFromIndex(index);

            if (container != null)
            {
                MarkContainerSelected(container, selected);
            }

            return container;
        }

        /// <summary>
        /// Called when an index is added to the <see cref="SelectedIndexes"/> collection.
        /// </summary>
        /// <param name="listIndex">The index in the SelectedIndexes collection.</param>
        /// <param name="itemIndexes">The item indexes.</param>
        private void SelectedIndexesAdded(int listIndex, IEnumerable<int> itemIndexes)
        {
            var indexes = (itemIndexes as IList<int>) ?? itemIndexes.ToList();
            IControl container = null;

            if (SelectedItems.Count != SelectedIndexes.Count)
            {
                var items = indexes.Select(x => Items.Cast<object>().ElementAt(x));
                SelectedItems.AddRange(items);
            }

            foreach (var itemIndex in indexes)
            {
                container = MarkIndexSelected(itemIndex, true);
            }

            if (SelectedIndexes.Count == 1)
            {
                RaisePropertyChanged(SelectedIndexProperty, -1, SelectedIndexes[0], BindingPriority.LocalValue);
            }

            if (container != null && Presenter?.Panel != null)
            {
                KeyboardNavigation.SetTabOnceActiveElement((InputElement)Presenter.Panel, container);
            }
        }

        /// <summary>
        /// Called when an index is removed from the <see cref="SelectedIndexes"/> collection.
        /// </summary>
        /// <param name="listIndex">The index in the SelectedIndexes collection.</param>
        /// <param name="itemIndexes">The item indexes.</param>
        private void SelectedIndexesRemoved(int listIndex, IEnumerable<int> itemIndexes)
        {
            var sync = SelectedIndexes.Count != SelectedItems.Count;

            foreach (var itemIndex in itemIndexes)
            {
                if (sync)
                {
                    SelectedItems.RemoveAt(listIndex++);
                }

                MarkIndexSelected(itemIndex, false);
            }

            if (SelectedIndexes.Count == 0)
            {
                RaisePropertyChanged(
                    SelectedIndexProperty, 
                    itemIndexes.First(), 
                    -1, 
                    BindingPriority.LocalValue);
            }
        }

        /// <summary>
        /// Called when an item is added to the <see cref="SelectedItems"/> collection.
        /// </summary>
        /// <param name="index">The index in the SelectedItems collection.</param>
        /// <param name="item">The item.</param>
        private void SelectedItemsAdded(int index, object item)
        {
            if (SelectedIndexes.Count != SelectedItems.Count)
            {
                SelectedIndexes.Insert(index, IndexOf(Items, item));
            }

            if (SelectedItems.Count == 1)
            {
                RaisePropertyChanged(SelectedItemProperty, null, item, BindingPriority.LocalValue);
            }
        }

        /// <summary>
        /// Called when an item is removed from the <see cref="SelectedItems"/> collection.
        /// </summary>
        /// <param name="index">The index in the SelectedItems collection.</param>
        /// <param name="item">The item.</param>
        private void SelectedItemsRemoved(int index, object item)
        {
            if (SelectedIndexes.Count != SelectedItems.Count)
            {
                SelectedIndexes.RemoveAt(index);
            }
        }

        /// <summary>
        /// Called when the <see cref="SelectedItems"/> collection is reset.
        /// </summary>
        private void SelectionReset()
        {
            if (SelectedIndexes.Count > 0)
            {
                SelectedIndexes.Clear();
            }

            if (SelectedItems.Count > 0)
            {
                SelectedItems.Clear();
            }

            foreach (var container in ItemContainerGenerator.Containers)
            {
                MarkContainerSelected(container, false);
            }
        }

        /// <summary>
        /// Validates items added to the <see cref="SelectedIndexes"/> collection.
        /// </summary>
        /// <param name="index">The index to be added.</param>
        private void ValidateIndex(int index)
        {
            if (index < 0 || index >= Items?.Cast<object>().Count())
            {
                throw new IndexOutOfRangeException();
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
    }
}
