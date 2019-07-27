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
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Logging;
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
                unsetValue: -1,
                defaultBindingMode: BindingMode.TwoWay);

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

        private static readonly IList Empty = Array.Empty<object>();

        private readonly Selection _selection = new Selection();
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
                    var effective = (value >= 0 && value < ItemCount) ? value : -1;
                    UpdateSelectedItem(effective);
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
                    UpdateSelectedItem(IndexOf(Items, value));
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
        /// <remarks>
        /// Note that the selection mode only applies to selections made via user interaction.
        /// Multiple selections can be made programatically regardless of the value of this property.
        /// </remarks>
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
                var newIndex = -1;

                if (SelectedIndex != -1)
                {
                    newIndex = IndexOf((IEnumerable)e.NewValue, SelectedItem);
                }

                if (AlwaysSelected && Items != null && Items.Cast<object>().Any())
                {
                    newIndex = 0;
                }

                SelectedIndex = newIndex;
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
                    else
                    {
                        _selection.ItemsInserted(e.NewStartingIndex, e.NewItems.Count);
                        UpdateSelectedItem(_selection.First(), false);
                    }

                    break;

                case NotifyCollectionChangedAction.Remove:
                    _selection.ItemsRemoved(e.OldStartingIndex, e.OldItems.Count);
                    UpdateSelectedItem(_selection.First(), false);
                    ResetSelectedItems();
                    break;

                case NotifyCollectionChangedAction.Replace:
                    UpdateSelectedItem(SelectedIndex, false);
                    ResetSelectedItems();
                    break;

                case NotifyCollectionChangedAction.Move:
                case NotifyCollectionChangedAction.Reset:
                    SelectedIndex = IndexOf(Items, SelectedItem);

                    if (AlwaysSelected && SelectedIndex == -1 && ItemCount > 0)
                    {
                        SelectedIndex = 0;
                    }
                    break;
            }
        }

        /// <inheritdoc/>
        protected override void OnContainersMaterialized(ItemContainerEventArgs e)
        {
            base.OnContainersMaterialized(e);

            var resetSelectedItems = false;

            foreach (var container in e.Containers)
            {
                if ((container.ContainerControl as ISelectable)?.IsSelected == true)
                {
                    if (SelectedIndex == -1)
                    {
                        SelectedIndex = container.Index;
                    }
                    else
                    {
                        if (_selection.Add(container.Index))
                        {
                            resetSelectedItems = true;
                        }
                    }

                    MarkContainerSelected(container.ContainerControl, true);
                }
                else if (_selection.Contains(container.Index))
                {
                    MarkContainerSelected(container.ContainerControl, true);
                }
            }

            if (resetSelectedItems)
            {
                ResetSelectedItems();
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
                    bool selected = _selection.Contains(i.Index);
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

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (!e.Handled)
            {
                var keymap = AvaloniaLocator.Current.GetService<PlatformHotkeyConfiguration>();
                bool Match(List<KeyGesture> gestures) => gestures.Any(g => g.Matches(e));

                if (ItemCount > 0 &&
                    Match(keymap.SelectAll) &&
                    (((SelectionMode & SelectionMode.Multiple) != 0) ||
                      (SelectionMode & SelectionMode.Toggle) != 0))
                {
                    SelectAll();
                    e.Handled = true;
                }
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
        /// Selects all items in the control.
        /// </summary>
        protected void SelectAll()
        {
            UpdateSelectedItems(() =>
            {
                _selection.Clear();

                for (var i = 0; i < ItemCount; ++i)
                {
                    _selection.Add(i);
                }

                UpdateSelectedItem(0, false);

                foreach (var container in ItemContainerGenerator.Containers)
                {
                    MarkItemSelected(container.Index, true);
                }

                ResetSelectedItems();
            });
        }

        /// <summary>
        /// Deselects all items in the control.
        /// </summary>
        protected void UnselectAll() => UpdateSelectedItem(-1);

        /// <summary>
        /// Updates the selection for an item based on user interaction.
        /// </summary>
        /// <param name="index">The index of the item.</param>
        /// <param name="select">Whether the item should be selected or unselected.</param>
        /// <param name="rangeModifier">Whether the range modifier is enabled (i.e. shift key).</param>
        /// <param name="toggleModifier">Whether the toggle modifier is enabled (i.e. ctrl key).</param>
        /// <param name="rightButton">Whether the event is a right-click.</param>
        protected void UpdateSelection(
            int index,
            bool select = true,
            bool rangeModifier = false,
            bool toggleModifier = false,
            bool rightButton = false)
        {
            if (index != -1)
            {
                if (select)
                {
                    var mode = SelectionMode;
                    var multi = (mode & SelectionMode.Multiple) != 0;
                    var toggle = (toggleModifier || (mode & SelectionMode.Toggle) != 0);
                    var range = multi && rangeModifier;

                    if (rightButton)
                    {
                        if (!_selection.Contains(index))
                        {
                            UpdateSelectedItem(index);
                        }
                    }
                    else if (range)
                    {
                        UpdateSelectedItems(() =>
                        {
                            var start = SelectedIndex != -1 ? SelectedIndex : 0;
                            var step = start < index ? 1 : -1;

                            _selection.Clear();

                            for (var i = start; i != index; i += step)
                            {
                                _selection.Add(i);
                            }

                            _selection.Add(index);

                            var first = Math.Min(start, index);
                            var last = Math.Max(start, index);

                            foreach (var container in ItemContainerGenerator.Containers)
                            {
                                MarkItemSelected(
                                    container.Index,
                                    container.Index >= first && container.Index <= last);
                            }

                            ResetSelectedItems();
                        });
                    }
                    else if (multi && toggle)
                    {
                        UpdateSelectedItems(() =>
                        {
                            if (!_selection.Contains(index))
                            {
                                _selection.Add(index);
                                MarkItemSelected(index, true);
                                SelectedItems.Add(ElementAt(Items, index));
                            }
                            else
                            {
                                _selection.Remove(index);
                                MarkItemSelected(index, false);

                                if (index == _selectedIndex)
                                {
                                    UpdateSelectedItem(_selection.First(), false);
                                }

                                SelectedItems.Remove(ElementAt(Items, index));
                            }
                        });
                    }
                    else if (toggle)
                    {
                        SelectedIndex = (SelectedIndex == index) ? -1 : index;
                    }
                    else
                    {
                        UpdateSelectedItem(index);
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
        /// <param name="rightButton">Whether the event is a right-click.</param>
        protected void UpdateSelection(
            IControl container,
            bool select = true,
            bool rangeModifier = false,
            bool toggleModifier = false,
            bool rightButton = false)
        {
            var index = ItemContainerGenerator?.IndexFromContainer(container) ?? -1;

            if (index != -1)
            {
                UpdateSelection(index, select, rangeModifier, toggleModifier, rightButton);
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
        /// <param name="rightButton">Whether the event is a right-click.</param>
        /// <returns>
        /// True if the event originated from a container that belongs to the control; otherwise
        /// false.
        /// </returns>
        protected bool UpdateSelectionFromEventSource(
            IInteractive eventSource,
            bool select = true,
            bool rangeModifier = false,
            bool toggleModifier = false,
            bool rightButton = false)
        {
            var container = GetContainerFromEventSource(eventSource);

            if (container != null)
            {
                UpdateSelection(container, select, rangeModifier, toggleModifier, rightButton);
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
        private static List<object> GetRange(IEnumerable items, int first, int last)
        {
            var list = (items as IList) ?? items.Cast<object>().ToList();
            var step = first > last ? -1 : 1;
            var result = new List<object>();

            for (int i = first; i != last; i += step)
            {
                result.Add(list[i]);
            }

            result.Add(list[last]);
            return result;
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
            var index = -1;

            if (items != null && AlwaysSelected)
            {
                index = Math.Min(SelectedIndex, items.Count() - 1);
            }

            SelectedIndex = index;
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
        private int MarkItemSelected(object item, bool selected)
        {
            var index = IndexOf(Items, item);

            if (index != -1)
            {
                MarkItemSelected(index, selected);
            }

            return index;
        }

        private void ResetSelectedItems()
        {
            UpdateSelectedItems(() =>
            {
                SelectedItems.Clear();

                foreach (var i in _selection)
                {
                    SelectedItems.Add(ElementAt(Items, i));
                }
            });
        }

        /// <summary>
        /// Called when the <see cref="SelectedItems"/> CollectionChanged event is raised.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void SelectedItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_syncingSelectedItems)
            {
                return;
            }

            void Add(IList newItems, IList addedItems = null)
            {
                foreach (var item in newItems)
                {
                    var index = MarkItemSelected(item, true);

                    if (index != -1 && _selection.Add(index) && addedItems != null)
                    {
                        addedItems.Add(item);
                    }
                }
            }

            void UpdateSelection()
            {
                if ((SelectedIndex != -1 && !_selection.Contains(SelectedIndex)) ||
                    (SelectedIndex == -1 && _selection.HasItems))
                {
                    _selectedIndex = _selection.First();
                    _selectedItem = ElementAt(Items, _selectedIndex);
                    RaisePropertyChanged(SelectedIndexProperty, -1, _selectedIndex, BindingPriority.LocalValue);
                    RaisePropertyChanged(SelectedItemProperty, null, _selectedItem, BindingPriority.LocalValue);

                    if (AutoScrollToSelectedItem)
                    {
                        ScrollIntoView(_selectedIndex);
                    }
                }
            }

            IList added = null;
            IList removed = null;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        Add(e.NewItems);
                        UpdateSelection();
                        added = e.NewItems;
                    }

                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (SelectedItems.Count == 0)
                    {
                        SelectedIndex = -1;
                    }

                    foreach (var item in e.OldItems)
                    {
                        var index = MarkItemSelected(item, false);
                        _selection.Remove(index);
                    }

                    removed = e.OldItems;
                    break;

                case NotifyCollectionChangedAction.Replace:
                    throw new NotSupportedException("Replacing items in a SelectedItems collection is not supported.");

                case NotifyCollectionChangedAction.Move:
                    throw new NotSupportedException("Moving items in a SelectedItems collection is not supported.");

                case NotifyCollectionChangedAction.Reset:
                    {
                        removed = new List<object>();
                        added = new List<object>();

                        foreach (var index in _selection.ToList())
                        {
                            var item = ElementAt(Items, index);

                            if (!SelectedItems.Contains(item))
                            {
                                MarkItemSelected(index, false);
                                removed.Add(item);
                                _selection.Remove(index);
                            }
                        }

                        Add(SelectedItems, added);
                        UpdateSelection();
                    }

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

        /// <summary>
        /// Updates the selection due to a change to <see cref="SelectedIndex"/> or
        /// <see cref="SelectedItem"/>.
        /// </summary>
        /// <param name="index">The new selected index.</param>
        /// <param name="clear">Whether to clear existing selection.</param>
        private void UpdateSelectedItem(int index, bool clear = true)
        {
            var oldIndex = _selectedIndex;
            var oldItem = _selectedItem;

            if (index == -1 && AlwaysSelected)
            {
                index = Math.Min(SelectedIndex, ItemCount - 1);
            }

            var item = ElementAt(Items, index);
            var added = -1;
            HashSet<int> removed = null;

            _selectedIndex = index;
            _selectedItem = item;

            if (oldIndex != index || _selection.HasMultiple)
            {
                if (clear)
                {
                    removed = _selection.Clear();
                }

                if (index != -1)
                {
                    if (_selection.Add(index))
                    {
                        added = index;
                    }

                    if (removed?.Contains(index) == true)
                    {
                        removed.Remove(index);
                        added = -1;
                    }
                }

                if (removed != null)
                {
                    foreach (var i in removed)
                    {
                        MarkItemSelected(i, false);
                    }
                }

                MarkItemSelected(index, true);

                RaisePropertyChanged(
                    SelectedIndexProperty,
                    oldIndex,
                    index);
            }

            if (!Equals(item, oldItem))
            {
                RaisePropertyChanged(
                    SelectedItemProperty,
                    oldItem,
                    item);
            }

            if (removed != null && index != -1)
            {
                removed.Remove(index);
            }

            if (added != -1 || removed?.Count > 0)
            {
                ResetSelectedItems();

                var e = new SelectionChangedEventArgs(
                    SelectionChangedEvent,
                    added != -1 ? new[] { ElementAt(Items, added) } : Array.Empty<object>(),
                    removed?.Select(x => ElementAt(Items, x)).ToArray() ?? Array.Empty<object>());
                RaiseEvent(e);
            }
        }

        private void UpdateSelectedItems(Action action)
        {
            try
            {
                _syncingSelectedItems = true;
                action();
            }
            catch (Exception ex)
            {
                Logger.Error(
                    LogArea.Property,
                    this,
                    "Error thrown updating SelectedItems: {Error}",
                    ex);
            }
            finally
            {
                _syncingSelectedItems = false;
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

        private class Selection : IEnumerable<int>
        {
            private readonly List<int> _list = new List<int>();
            private HashSet<int> _set = new HashSet<int>();

            public bool HasItems => _set.Count > 0;
            public bool HasMultiple => _set.Count > 1;

            public bool Add(int index)
            {
                if (index == -1)
                {
                    throw new ArgumentException("Invalid index", "index");
                }

                if (_set.Add(index))
                {
                    _list.Add(index);
                    return true;
                }

                return false;
            }

            public bool Remove(int index)
            {
                if (_set.Remove(index))
                {
                    _list.RemoveAll(x => x == index);
                    return true;
                }

                return false;
            }

            public HashSet<int> Clear()
            {
                var result = _set;
                _list.Clear();
                _set = new HashSet<int>();
                return result;
            }

            public void ItemsInserted(int index, int count)
            {
                _set = new HashSet<int>();

                for (var i = 0; i < _list.Count; ++i)
                {
                    var ix = _list[i];

                    if (ix >= index)
                    {
                        var newIndex = ix + count;
                        _list[i] = newIndex;
                        _set.Add(newIndex);
                    }
                    else
                    {
                        _set.Add(ix);
                    }
                }
            }

            public void ItemsRemoved(int index, int count)
            {
                var last = (index + count) - 1;

                _set = new HashSet<int>();

                for (var i = 0; i < _list.Count; ++i)
                {
                    var ix = _list[i];

                    if (ix >= index && ix <= last)
                    {
                        _list.RemoveAt(i--);
                    }
                    else if (ix > last)
                    {
                        var newIndex = ix - count;
                        _list[i] = newIndex;
                        _set.Add(newIndex);
                    }
                    else
                    {
                        _set.Add(ix);
                    }
                }
            }

            public bool Contains(int index) => _set.Contains(index);

            public int First() => HasItems ? _list[0] : -1;

            public IEnumerator<int> GetEnumerator() => _set.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
