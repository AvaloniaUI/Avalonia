// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    /// <summary>
    /// Displays a hierarchical tree of data.
    /// </summary>
    public class TreeView : ItemsControl, ICustomKeyboardNavigation
    {
        /// <summary>
        /// Defines the <see cref="AutoScrollToSelectedItem"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> AutoScrollToSelectedItemProperty =
            SelectingItemsControl.AutoScrollToSelectedItemProperty.AddOwner<TreeView>();

        /// <summary>
        /// Defines the <see cref="SelectedItem"/> property.
        /// </summary>
        public static readonly DirectProperty<TreeView, object> SelectedItemProperty =
            SelectingItemsControl.SelectedItemProperty.AddOwner<TreeView>(
                o => o.SelectedItem,
                (o, v) => o.SelectedItem = v);

        /// <summary>
        /// Defines the <see cref="SelectedItems"/> property.
        /// </summary>
        public static readonly DirectProperty<TreeView, IList> SelectedItemsProperty =
            ListBox.SelectedItemsProperty.AddOwner<TreeView>(
                o => o.SelectedItems,
                (o, v) => o.SelectedItems = v);

        /// <summary>
        /// Defines the <see cref="SelectionMode"/> property.
        /// </summary>
        public static readonly StyledProperty<SelectionMode> SelectionModeProperty =
            ListBox.SelectionModeProperty.AddOwner<TreeView>();

        private static readonly IList Empty = new object[0];
        private object _selectedItem;
        private IList _selectedItems;

        /// <summary>
        /// Initializes static members of the <see cref="TreeView"/> class.
        /// </summary>
        static TreeView()
        {
            // HACK: Needed or SelectedItem property will not be found in Release build.
        }

        /// <summary>
        /// Occurs when the control's selection changes.
        /// </summary>
        public event EventHandler<SelectionChangedEventArgs> SelectionChanged
        {
            add => AddHandler(SelectingItemsControl.SelectionChangedEvent, value);
            remove => RemoveHandler(SelectingItemsControl.SelectionChangedEvent, value);
        }

        /// <summary>
        /// Gets the <see cref="ITreeItemContainerGenerator"/> for the tree view.
        /// </summary>
        public new ITreeItemContainerGenerator ItemContainerGenerator =>
            (ITreeItemContainerGenerator)base.ItemContainerGenerator;

        /// <summary>
        /// Gets or sets a value indicating whether to automatically scroll to newly selected items.
        /// </summary>
        public bool AutoScrollToSelectedItem
        {
            get => GetValue(AutoScrollToSelectedItemProperty);
            set => SetValue(AutoScrollToSelectedItemProperty, value);
        }

        private bool _syncingSelectedItems;

        /// <summary>
        /// Gets or sets the selection mode.
        /// </summary>
        public SelectionMode SelectionMode
        {
            get => GetValue(SelectionModeProperty);
            set => SetValue(SelectionModeProperty, value);
        }

        /// <summary>
        /// Gets or sets the selected item.
        /// </summary>
        public object SelectedItem
        {
            get => _selectedItem;
            set
            {
                SetAndRaise(SelectedItemProperty, ref _selectedItem,
                    (object val, ref object backing, Action<Action> notifyWrapper) =>
                    {
                        var old = backing;
                        backing = val;

                        notifyWrapper(() =>
                            RaisePropertyChanged(
                                SelectedItemProperty,
                                old,
                                val));

                        if (val != null)
                        {
                            if (SelectedItems.Count != 1 || SelectedItems[0] != val)
                            {
                                _syncingSelectedItems = true;
                                SelectSingleItem(val);
                                _syncingSelectedItems = false;
                            }
                        }
                        else if (SelectedItems.Count > 0)
                        {
                            SelectedItems.Clear();
                        }
                    }, value);
            }
        }

        /// <summary>
        /// Gets the selected items.
        /// </summary>
        public IList SelectedItems
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
        /// Subscribes to the <see cref="SelectedItems"/> CollectionChanged event, if any.
        /// </summary>
        private void SubscribeToSelectedItems()
        {
            if (_selectedItems is INotifyCollectionChanged incc)
            {
                incc.CollectionChanged += SelectedItemsCollectionChanged;
            }

            SelectedItemsCollectionChanged(
                _selectedItems,
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        private void SelectSingleItem(object item)
        {
            SelectedItems.Clear();
            SelectedItems.Add(item);
        }

        /// <summary>
        /// Called when the <see cref="SelectedItems"/> CollectionChanged event is raised.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void SelectedItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            IList added = null;
            IList removed = null;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:

                    SelectedItemsAdded(e.NewItems.Cast<object>().ToArray());

                    if (AutoScrollToSelectedItem)
                    {
                        var container = (TreeViewItem)ItemContainerGenerator.Index.ContainerFromItem(e.NewItems[0]);

                        container?.BringIntoView();
                    }

                    added = e.NewItems;

                    break;
                case NotifyCollectionChangedAction.Remove:

                    if (!_syncingSelectedItems)
                    {
                        if (SelectedItems.Count == 0)
                        {
                            SelectedItem = null;
                        }
                        else
                        {
                            var selectedIndex = SelectedItems.IndexOf(_selectedItem);

                            if (selectedIndex == -1)
                            {
                                var old = _selectedItem;
                                _selectedItem = SelectedItems[0];

                                RaisePropertyChanged(SelectedItemProperty, old, _selectedItem);
                            }
                        }
                    }

                    foreach (var item in e.OldItems)
                    {
                        MarkItemSelected(item, false);
                    }

                    removed = e.OldItems;

                    break;
                case NotifyCollectionChangedAction.Reset:

                    foreach (IControl container in ItemContainerGenerator.Index.Containers)
                    {
                        MarkContainerSelected(container, false);
                    }

                    if (SelectedItems.Count > 0)
                    {
                        SelectedItemsAdded(SelectedItems);

                        added = SelectedItems;
                    }
                    else if (!_syncingSelectedItems)
                    {
                        SelectedItem = null;
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
                        var item = SelectedItems[0];
                        _selectedItem = item;
                        RaisePropertyChanged(SelectedItemProperty, oldItem, item);
                    }

                    added = e.NewItems;
                    removed = e.OldItems;

                    break;
            }

            if (added?.Count > 0 || removed?.Count > 0)
            {
                var changed = new SelectionChangedEventArgs(
                    SelectingItemsControl.SelectionChangedEvent,
                    added ?? Empty,
                    removed ?? Empty);
                RaiseEvent(changed);
            }
        }

        private void MarkItemSelected(object item, bool selected)
        {
            var container = ItemContainerGenerator.Index.ContainerFromItem(item);

            MarkContainerSelected(container, selected);
        }

        private void SelectedItemsAdded(IList items)
        {
            if (items.Count == 0)
            {
                return;
            }

            foreach (object item in items)
            {
                MarkItemSelected(item, true);
            }

            if (SelectedItem == null && !_syncingSelectedItems)
            {
                SetAndRaise(SelectedItemProperty, ref _selectedItem, items[0]);
            }
        }

        /// <summary>
        /// Unsubscribes from the <see cref="SelectedItems"/> CollectionChanged event, if any.
        /// </summary>
        private void UnsubscribeFromSelectedItems()
        {
            if (_selectedItems is INotifyCollectionChanged incc)
            {
                incc.CollectionChanged -= SelectedItemsCollectionChanged;
            }
        }

        (bool handled, IInputElement next) ICustomKeyboardNavigation.GetNext(IInputElement element,
            NavigationDirection direction)
        {
            if (direction == NavigationDirection.Next || direction == NavigationDirection.Previous)
            {
                if (!this.IsVisualAncestorOf(element))
                {
                    IControl result = _selectedItem != null ?
                        ItemContainerGenerator.Index.ContainerFromItem(_selectedItem) :
                        ItemContainerGenerator.ContainerFromIndex(0);
                    return (true, result);
                }

                return (true, null);
            }

            return (false, null);
        }

        /// <inheritdoc/>
        protected override IItemContainerGenerator CreateItemContainerGenerator()
        {
            var result = new TreeItemContainerGenerator<TreeViewItem>(
                this,
                TreeViewItem.HeaderProperty,
                TreeViewItem.ItemTemplateProperty,
                TreeViewItem.ItemsProperty,
                TreeViewItem.IsExpandedProperty,
                new TreeContainerIndex());
            result.Index.Materialized += ContainerMaterialized;
            return result;
        }

        /// <inheritdoc/>
        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            if (e.NavigationMethod == NavigationMethod.Directional)
            {
                e.Handled = UpdateSelectionFromEventSource(
                    e.Source,
                    true,
                    (e.InputModifiers & InputModifiers.Shift) != 0);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            var direction = e.Key.ToNavigationDirection();

            if (direction?.IsDirectional() == true && !e.Handled)
            {
                if (SelectedItem != null)
                {
                    var next = GetContainerInDirection(
                        GetContainerFromEventSource(e.Source),
                        direction.Value,
                        true);

                    if (next != null)
                    {
                        next.Focus(NavigationMethod.Directional);
                        e.Handled = true;
                    }
                }
                else
                {
                    SelectedItem = ElementAt(Items, 0);
                }
            }

            if (!e.Handled)
            {
                var keymap = AvaloniaLocator.Current.GetService<PlatformHotkeyConfiguration>();
                bool Match(List<KeyGesture> gestures) => gestures.Any(g => g.Matches(e));

                if (this.SelectionMode == SelectionMode.Multiple && Match(keymap.SelectAll))
                {
                    SelectingItemsControl.SynchronizeItems(SelectedItems, ItemContainerGenerator.Index.Items);
                    e.Handled = true;
                }
            }
        }

        private TreeViewItem GetContainerInDirection(
            TreeViewItem from,
            NavigationDirection direction,
            bool intoChildren)
        {
            IItemContainerGenerator parentGenerator = GetParentContainerGenerator(from);

            if (parentGenerator == null)
            {
                return null;
            }

            var index = parentGenerator.IndexFromContainer(from);
            var parent = from.Parent as ItemsControl;
            TreeViewItem result = null;

            switch (direction)
            {
                case NavigationDirection.Up:
                    if (index > 0)
                    {
                        var previous = (TreeViewItem)parentGenerator.ContainerFromIndex(index - 1);
                        result = previous.IsExpanded ?
                            (TreeViewItem)previous.ItemContainerGenerator.ContainerFromIndex(previous.ItemCount - 1) :
                            previous;
                    }
                    else
                    {
                        result = from.Parent as TreeViewItem;
                    }

                    break;

                case NavigationDirection.Down:
                    if (from.IsExpanded && intoChildren)
                    {
                        result = (TreeViewItem)from.ItemContainerGenerator.ContainerFromIndex(0);
                    }
                    else if (index < parent?.ItemCount - 1)
                    {
                        result = (TreeViewItem)parentGenerator.ContainerFromIndex(index + 1);
                    }
                    else if (parent is TreeViewItem parentItem)
                    {
                        return GetContainerInDirection(parentItem, direction, false);
                    }

                    break;
            }

            return result;
        }

        /// <inheritdoc/>
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            if (e.MouseButton == MouseButton.Left || e.MouseButton == MouseButton.Right)
            {
                e.Handled = UpdateSelectionFromEventSource(
                    e.Source,
                    true,
                    (e.InputModifiers & InputModifiers.Shift) != 0,
                    (e.InputModifiers & InputModifiers.Control) != 0);
            }
        }

        /// <summary>
        /// Updates the selection for an item based on user interaction.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="select">Whether the item should be selected or unselected.</param>
        /// <param name="rangeModifier">Whether the range modifier is enabled (i.e. shift key).</param>
        /// <param name="toggleModifier">Whether the toggle modifier is enabled (i.e. ctrl key).</param>
        protected void UpdateSelectionFromContainer(
            IControl container,
            bool select = true,
            bool rangeModifier = false,
            bool toggleModifier = false)
        {
            var item = ItemContainerGenerator.Index.ItemFromContainer(container);

            if (item == null)
            {
                return;
            }

            IControl selectedContainer = null;

            if (SelectedItem != null)
            {
                selectedContainer = ItemContainerGenerator.Index.ContainerFromItem(SelectedItem);
            }

            var mode = SelectionMode;
            var toggle = toggleModifier || (mode & SelectionMode.Toggle) != 0;
            var multi = (mode & SelectionMode.Multiple) != 0;
            var range = multi && selectedContainer != null && rangeModifier;

            if (!toggle && !range)
            {
                SelectSingleItem(item);
            }
            else if (multi && range)
            {
                SelectingItemsControl.SynchronizeItems(
                    SelectedItems,
                    GetItemsInRange(selectedContainer as TreeViewItem, container as TreeViewItem));
            }
            else
            {
                var i = SelectedItems.IndexOf(item);

                if (i != -1)
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
                        SelectedItem = item;
                    }
                }
            }
        }

        private static IItemContainerGenerator GetParentContainerGenerator(TreeViewItem item)
        {
            if (item == null)
            {
                return null;
            }

            switch (item.Parent)
            {
                case TreeView treeView:
                    return treeView.ItemContainerGenerator;
                case TreeViewItem treeViewItem:
                    return treeViewItem.ItemContainerGenerator;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Find which node is first in hierarchy.
        /// </summary>
        /// <param name="treeView">Search root.</param>
        /// <param name="nodeA">Nodes to find.</param>
        /// <param name="nodeB">Node to find.</param>
        /// <returns>Found first node.</returns>
        private static TreeViewItem FindFirstNode(TreeView treeView, TreeViewItem nodeA, TreeViewItem nodeB)
        {
            return FindInContainers(treeView.ItemContainerGenerator, nodeA, nodeB);
        }

        private static TreeViewItem FindInContainers(ITreeItemContainerGenerator containerGenerator,
            TreeViewItem nodeA,
            TreeViewItem nodeB)
        {
            IEnumerable<ItemContainerInfo> containers = containerGenerator.Containers;

            foreach (ItemContainerInfo container in containers)
            {
                TreeViewItem node = FindFirstNode(container.ContainerControl as TreeViewItem, nodeA, nodeB);

                if (node != null)
                {
                    return node;
                }
            }

            return null;
        }

        private static TreeViewItem FindFirstNode(TreeViewItem node, TreeViewItem nodeA, TreeViewItem nodeB)
        {
            if (node == null)
            {
                return null;
            }

            TreeViewItem match = node == nodeA ? nodeA : node == nodeB ? nodeB : null;

            if (match != null)
            {
                return match;
            }

            return FindInContainers(node.ItemContainerGenerator, nodeA, nodeB);
        }

        /// <summary>
        /// Returns all items that belong to containers between <paramref name="from"/> and <paramref name="to"/>.
        /// The range is inclusive.
        /// </summary>
        /// <param name="from">From container.</param>
        /// <param name="to">To container.</param>
        private List<object> GetItemsInRange(TreeViewItem from, TreeViewItem to)
        {
            var items = new List<object>();

            if (from == null || to == null)
            {
                return items;
            }

            TreeViewItem firstItem = FindFirstNode(this, from, to);

            if (firstItem == null)
            {
                return items;
            }

            bool wasReversed = false;

            if (firstItem == to)
            {
                var temp = from;

                from = to;
                to = temp;

                wasReversed = true;
            }

            TreeViewItem node = from;

            while (node != to)
            {
                var item = ItemContainerGenerator.Index.ItemFromContainer(node);

                if (item != null)
                {
                    items.Add(item);
                }

                node = GetContainerInDirection(node, NavigationDirection.Down, true);
            }

            var toItem = ItemContainerGenerator.Index.ItemFromContainer(to);

            if (toItem != null)
            {
                items.Add(toItem);
            }

            if (wasReversed)
            {
                items.Reverse();
            }

            return items;
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
                UpdateSelectionFromContainer(container, select, rangeModifier, toggleModifier);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tries to get the container that was the source of an event.
        /// </summary>
        /// <param name="eventSource">The control that raised the event.</param>
        /// <returns>The container or null if the event did not originate in a container.</returns>
        protected TreeViewItem GetContainerFromEventSource(IInteractive eventSource)
        {
            var item = ((IVisual)eventSource).GetSelfAndVisualAncestors()
                .OfType<TreeViewItem>()
                .FirstOrDefault();

            if (item != null)
            {
                if (item.ItemContainerGenerator.Index == ItemContainerGenerator.Index)
                {
                    return item;
                }
            }

            return null;
        }

        /// <summary>
        /// Called when a new item container is materialized, to set its selected state.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void ContainerMaterialized(object sender, ItemContainerEventArgs e)
        {
            var selectedItem = SelectedItem;

            if (selectedItem == null)
            {
                return;
            }

            foreach (var container in e.Containers)
            {
                if (container.Item == selectedItem)
                {
                    ((TreeViewItem)container.ContainerControl).IsSelected = true;

                    if (AutoScrollToSelectedItem)
                    {
                        Dispatcher.UIThread.Post(container.ContainerControl.BringIntoView);
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// Sets a container's 'selected' class or <see cref="ISelectable.IsSelected"/>.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="selected">Whether the control is selected</param>
        private void MarkContainerSelected(IControl container, bool selected)
        {
            if (container == null)
            {
                return;
            }

            if (container is ISelectable selectable)
            {
                selectable.IsSelected = selected;
            }
            else
            {
                container.Classes.Set(":selected", selected);
            }
        }
    }
}
