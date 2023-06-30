
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Layout;
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
        public static readonly DirectProperty<TreeView, object?> SelectedItemProperty =
            SelectingItemsControl.SelectedItemProperty.AddOwner<TreeView>(
                o => o.SelectedItem,
                (o, v) => o.SelectedItem = v);

        /// <summary>
        /// Defines the <see cref="SelectedItems"/> property.
        /// </summary>
        public static readonly DirectProperty<TreeView, IList> SelectedItemsProperty =
            AvaloniaProperty.RegisterDirect<TreeView, IList>(
                nameof(SelectedItems),
                o => o.SelectedItems,
                (o, v) => o.SelectedItems = v);

        /// <summary>
        /// Defines the <see cref="SelectionMode"/> property.
        /// </summary>
        public static readonly StyledProperty<SelectionMode> SelectionModeProperty =
            ListBox.SelectionModeProperty.AddOwner<TreeView>();

        private static readonly IList Empty = Array.Empty<object>();
        private object? _selectedItem;
        private IList? _selectedItems;
        private bool _syncingSelectedItems;

        /// <summary>
        /// Initializes static members of the <see cref="TreeView"/> class.
        /// </summary>
        static TreeView()
        {
            SelectingItemsControl.IsSelectedChangedEvent.AddClassHandler<TreeView>((x, e) =>
                x.ContainerSelectionChanged(e));
        }

        /// <summary>
        /// Occurs when the control's selection changes.
        /// </summary>
        public event EventHandler<SelectionChangedEventArgs>? SelectionChanged
        {
            add => AddHandler(SelectingItemsControl.SelectionChangedEvent, value);
            remove => RemoveHandler(SelectingItemsControl.SelectionChangedEvent, value);
        }

        /// <summary>
        /// Gets the <see cref="TreeItemContainerGenerator"/> for the tree view.
        /// </summary>
        public new TreeItemContainerGenerator ItemContainerGenerator =>
            (TreeItemContainerGenerator)base.ItemContainerGenerator;

        /// <summary>
        /// Gets or sets a value indicating whether to automatically scroll to newly selected items.
        /// </summary>
        /// <remarks>
        /// This property is of limited use with <see cref="TreeView"/> as it will only scroll
        /// to realized items. To scroll to a non-expanded item, you need to ensure that its
        /// ancestors are expanded.
        /// </remarks>
        public bool AutoScrollToSelectedItem
        {
            get => GetValue(AutoScrollToSelectedItemProperty);
            set => SetValue(AutoScrollToSelectedItemProperty, value);
        }

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
        /// <remarks>
        /// Note that setting this property only currently works if the item is expanded to be visible.
        /// To select non-expanded nodes use `Selection.SelectedIndex`.
        /// </remarks>
        public object? SelectedItem
        {
            get => _selectedItem;
            set
            {
                var selectedItems = SelectedItems;

                SetAndRaise(SelectedItemProperty, ref _selectedItem, value);

                if (value != null)
                {
                    if (selectedItems.Count != 1 || selectedItems[0] != value)
                    {                        
                        SelectSingleItem(value);                        
                    }
                }
                else if (SelectedItems.Count > 0)
                {
                    SelectedItems.Clear();
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected items.
        /// </summary>
        [AllowNull]
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
        /// Expands the specified <see cref="TreeViewItem"/> all descendent <see cref="TreeViewItem"/>s.
        /// </summary>
        /// <param name="item">The item to expand.</param>
        public void ExpandSubTree(TreeViewItem item)
        {
            item.IsExpanded = true;

            if (item.Presenter?.Panel is null)
                (this.GetVisualRoot() as ILayoutRoot)?.LayoutManager.ExecuteLayoutPass();

            if (item.Presenter?.Panel is { } panel)
            {
                foreach (var child in panel.Children)
                {
                    if (child is TreeViewItem treeViewItem)
                    {
                        ExpandSubTree(treeViewItem);
                    }
                }
            }
        }

        /// <summary>
        /// Collapse the specified <see cref="TreeViewItem"/> all descendent <see cref="TreeViewItem"/> s.
        /// </summary>
        /// <param name="item">The item to collapse.</param>
        public void CollapseSubTree(TreeViewItem item)
        {
            item.IsExpanded = false;

            if (item.Presenter?.Panel != null)
            {
                foreach (var child in item.Presenter.Panel.Children)
                {
                    if (child is TreeViewItem treeViewItem)
                    {
                        CollapseSubTree(treeViewItem);
                    }
                }
            }
        }

        /// <summary>
        /// Selects all items in the <see cref="TreeView"/>.
        /// </summary>
        /// <remarks>
        /// Note that this method only selects nodes currently visible due to their parent nodes
        /// being expanded: it does not expand nodes.
        /// </remarks>
        public void SelectAll()
        {
            var allItems = new List<object>();

            void AddItems(ItemsControl itemsControl)
            {
                foreach (var item in itemsControl.ItemsView)
                    allItems.Add(item!);

                foreach (var child in itemsControl.GetRealizedContainers())
                {
                    if (child is ItemsControl childItemsControl)
                        AddItems(childItemsControl);
                }
            }

            AddItems(this);
            SynchronizeItems(SelectedItems, allItems);
        }

        /// <summary>
        /// Deselects all items in the <see cref="TreeView"/>.
        /// </summary>
        public void UnselectAll()
        {
            SelectedItems.Clear();
        }

        public IEnumerable<Control> GetRealizedTreeContainers()
        {
            static IEnumerable<Control> GetRealizedContainers(ItemsControl itemsControl)
            {
                foreach (var container in itemsControl.GetRealizedContainers())
                {
                    yield return container;
                    if (container is ItemsControl itemsControlContainer)
                        foreach (var child in GetRealizedContainers(itemsControlContainer))
                            yield return child;
                }
            }

            return GetRealizedContainers(this);
        }

        public Control? TreeContainerFromItem(object item)
        {
            static Control? TreeContainerFromItem(ItemsControl itemsControl, object item)
            {
                if (itemsControl.ContainerFromItem(item) is { } container)
                    return container;

                foreach (var child in itemsControl.GetRealizedContainers())
                {
                    if (child is ItemsControl childItemsControl &&
                        TreeContainerFromItem(childItemsControl, item) is { } childContainer)
                        return childContainer;
                }

                return null;
            }

            return TreeContainerFromItem(this, item);
        }

        public object? TreeItemFromContainer(Control container)
        {
            static object? TreeItemFromContainer(ItemsControl itemsControl, Control container)
            {
                if (itemsControl.ItemFromContainer(container) is { } item)
                    return item;

                foreach (var child in itemsControl.GetRealizedContainers())
                {
                    if (child is ItemsControl childItemsControl &&
                        TreeItemFromContainer(childItemsControl, container) is { } childContainer)
                        return childContainer;
                }

                return null;
            }

            return TreeItemFromContainer(this, container);
        }

        private protected override void OnItemsViewCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsViewCollectionChanged(sender, e);

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Replace:
                    foreach (var i in e.OldItems!)
                        SelectedItems.Remove(i);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    SelectedItems.Clear();
                    break;
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
            var oldValue = _selectedItem;
            _syncingSelectedItems = true;
            SelectedItems.Clear();
            _selectedItem = item;
            SelectedItems.Add(item);
            _syncingSelectedItems = false;

            RaisePropertyChanged(SelectedItemProperty, oldValue, _selectedItem);    
        }

        /// <summary>
        /// Called when the <see cref="SelectedItems"/> CollectionChanged event is raised.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void SelectedItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            IList? added = null;
            IList? removed = null;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:

                    SelectedItemsAdded(e.NewItems!.Cast<object>().ToArray());

                    var selectedItem = SelectedItem;

                    if (AutoScrollToSelectedItem && 
                        selectedItem is not null &&
                        e.NewItems![0] == selectedItem)
                    {
                        var container = TreeContainerFromItem(selectedItem);

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

                    foreach (var item in e.OldItems!)
                    {
                        MarkItemSelected(item, false);
                    }

                    removed = e.OldItems;

                    break;
                case NotifyCollectionChangedAction.Reset:

                    foreach (var container in GetRealizedTreeContainers())
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

                    foreach (var item in e.OldItems!)
                    {
                        MarkItemSelected(item, false);
                    }

                    foreach (var item in e.NewItems!)
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
                    removed ?? Empty,
                    added ?? Empty);
                RaiseEvent(changed);
            }
        }

        private void MarkItemSelected(object item, bool selected)
        {
            if (TreeContainerFromItem(item) is Control container)
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
        
        (bool handled, IInputElement? next) ICustomKeyboardNavigation.GetNext(IInputElement element,
            NavigationDirection direction)
        {
            if (direction == NavigationDirection.Next || direction == NavigationDirection.Previous)
            {
                if (!this.IsVisualAncestorOf((Visual)element))
                {
                    var result = _selectedItem != null ?
                        TreeContainerFromItem(_selectedItem) :
                        ContainerFromIndex(0);
                    
                    return (result != null, result); // SelectedItem may not be in the treeview.
                }

                return (true, null);
            }

            return (false, null);
        }

        protected internal override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
        {
            return new TreeViewItem();
        }

        protected internal override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
        {
            return NeedsContainer<TreeViewItem>(item, out recycleKey);
        }

        protected internal override void ContainerForItemPreparedOverride(Control container, object? item, int index)
        {
            base.ContainerForItemPreparedOverride(container, item, index);

            // Once the container has been full prepared and added to the tree, any bindings from
            // styles or item container themes are guaranteed to be applied. 
            if (container.IsSet(SelectingItemsControl.IsSelectedProperty))
            {
                // The IsSelected property is set on the container: there is a style or item
                // container theme which has bound the IsSelected property. Update our selection
                // based on the selection state of the container.
                var containerIsSelected = SelectingItemsControl.GetIsSelected(container);
                UpdateSelectionFromContainer(container, select: containerIsSelected, toggleModifier: true);
            }

            // The IsSelected property is not set on the container: update the container
            // selection based on the current selection as understood by this control.
            MarkContainerSelected(container, SelectedItems.Contains(item));

            // If the newly realized container is the selected container, scroll to it after layout.
            if (AutoScrollToSelectedItem && SelectedItem == item)
            {
                Dispatcher.UIThread.Post(container.BringIntoView, DispatcherPriority.Loaded);
            }
        }

        /// <inheritdoc/>
        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            if (e.NavigationMethod == NavigationMethod.Directional)
            {
                e.Handled = UpdateSelectionFromEventSource(
                    e.Source!,
                    true,
                    e.KeyModifiers.HasAllFlags(KeyModifiers.Shift));
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
                        GetContainerFromEventSource(e.Source!),
                        direction.Value,
                        true);

                    if (next != null)
                    {
                        e.Handled = next.Focus(NavigationMethod.Directional);
                    }
                }
                else
                {
                    SelectedItem = ItemsView[0];
                }
            }

            if (!e.Handled)
            {
                var keymap = Application.Current!.PlatformSettings!.HotkeyConfiguration;
                bool Match(List<KeyGesture>? gestures) => gestures?.Any(g => g.Matches(e)) ?? false;

                if (this.SelectionMode == SelectionMode.Multiple && Match(keymap?.SelectAll))
                {
                    SelectAll();
                    e.Handled = true;
                }
            }
        }

        private TreeViewItem? GetContainerInDirection(
            TreeViewItem? from,
            NavigationDirection direction,
            bool intoChildren)
        {
            var parentItemsControl = from?.Parent switch
            {
                TreeView tv => (ItemsControl)tv,
                TreeViewItem i => i,
                _ => null
            };

            if (parentItemsControl == null)
            {
                return null;
            }

            var index = from is not null ? parentItemsControl.IndexFromContainer(from) : -1;
            var parent = from?.Parent as ItemsControl;
            TreeViewItem? result = null;

            switch (direction)
            {
                case NavigationDirection.Up:
                    if (index > 0)
                    {
                        var previous = (TreeViewItem)parentItemsControl.ContainerFromIndex(index - 1)!;
                        result = previous.IsExpanded && previous.ItemCount > 0 ?
                            (TreeViewItem)previous.ContainerFromIndex(previous.ItemCount - 1)! :
                            previous;
                    }
                    else
                    {
                        result = from?.Parent as TreeViewItem;
                    }

                    break;

                case NavigationDirection.Down:
                case NavigationDirection.Right:
                    if (from?.IsExpanded == true && intoChildren && from.ItemCount > 0)
                    {
                        result = (TreeViewItem)from.ContainerFromIndex(0)!;
                    }
                    else if (index < parent?.ItemCount - 1)
                    {
                        result = (TreeViewItem)parentItemsControl.ContainerFromIndex(index + 1)!;
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

            if (e.Source is Visual source)
            {
                var point = e.GetCurrentPoint(source);

                if (point.Properties.IsLeftButtonPressed || point.Properties.IsRightButtonPressed)
                {
                    var keymap = Application.Current!.PlatformSettings!.HotkeyConfiguration;
                    e.Handled = UpdateSelectionFromEventSource(
                        e.Source,
                        true,
                        e.KeyModifiers.HasAllFlags(KeyModifiers.Shift),
                        e.KeyModifiers.HasAllFlags(keymap.CommandModifiers),
                        point.Properties.IsRightButtonPressed);
                }
            }
        }

        /// <summary>
        /// Updates the selection for an item based on user interaction.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="select">Whether the item should be selected or unselected.</param>
        /// <param name="rangeModifier">Whether the range modifier is enabled (i.e. shift key).</param>
        /// <param name="toggleModifier">Whether the toggle modifier is enabled (i.e. ctrl key).</param>
        /// <param name="rightButton">Whether the event is a right-click.</param>
        protected void UpdateSelectionFromContainer(
            Control container,
            bool select = true,
            bool rangeModifier = false,
            bool toggleModifier = false,
            bool rightButton = false)
        {
            var item = TreeItemFromContainer(container);

            if (item == null)
            {
                return;
            }

            Control? selectedContainer = null;

            if (SelectedItem != null)
            {
                selectedContainer = TreeContainerFromItem(SelectedItem);
            }

            var mode = SelectionMode;
            var toggle = toggleModifier || mode.HasAllFlags(SelectionMode.Toggle);
            var multi = mode.HasAllFlags(SelectionMode.Multiple);
            var range = multi && rangeModifier && selectedContainer != null;

            if (!select)
            {
                SelectedItems.Remove(item);
            }
            else if (rightButton)
            {
                if (!SelectedItems.Contains(item))
                {
                    SelectSingleItem(item);
                }
            }
            else if (!toggle && !range)
            {
                SelectSingleItem(item);
            }
            else if (multi && range)
            {
                SynchronizeItems(
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

        [Obsolete, EditorBrowsable(EditorBrowsableState.Never)]
        private protected override ItemContainerGenerator CreateItemContainerGenerator()
        {
            return new TreeItemContainerGenerator(this);
        }

        /// <summary>
        /// Find which node is first in hierarchy.
        /// </summary>
        /// <param name="treeView">Search root.</param>
        /// <param name="nodeA">Nodes to find.</param>
        /// <param name="nodeB">Node to find.</param>
        /// <returns>Found first node.</returns>
        private static TreeViewItem? FindFirstNode(TreeView treeView, TreeViewItem nodeA, TreeViewItem nodeB)
        {
            return FindInContainers(treeView, nodeA, nodeB);
        }

        private static TreeViewItem? FindInContainers(ItemsControl itemsControl,
            TreeViewItem nodeA,
            TreeViewItem nodeB)
        {
            foreach (var container in itemsControl.GetRealizedContainers())
            {
                TreeViewItem? node = FindFirstNode(container as TreeViewItem, nodeA, nodeB);

                if (node != null)
                {
                    return node;
                }
            }

            return null;
        }

        private static TreeViewItem? FindFirstNode(TreeViewItem? node, TreeViewItem nodeA, TreeViewItem nodeB)
        {
            if (node == null)
            {
                return null;
            }

            TreeViewItem? match = node == nodeA ? nodeA : node == nodeB ? nodeB : null;

            if (match != null)
            {
                return match;
            }

            return FindInContainers(node, nodeA, nodeB);
        }

        /// <summary>
        /// Returns all items that belong to containers between <paramref name="from"/> and <paramref name="to"/>.
        /// The range is inclusive.
        /// </summary>
        /// <param name="from">From container.</param>
        /// <param name="to">To container.</param>
        private List<object> GetItemsInRange(TreeViewItem? from, TreeViewItem? to)
        {
            var items = new List<object>();

            if (from == null || to == null)
            {
                return items;
            }

            TreeViewItem? firstItem = FindFirstNode(this, from, to);

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

            TreeViewItem? node = from;

            while (node is not null && node != to)
            {
                var item = TreeItemFromContainer(node);

                if (item != null)
                {
                    items.Add(item);
                }

                node = GetContainerInDirection(node, NavigationDirection.Down, true);
            }

            var toItem = TreeItemFromContainer(to);

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
        /// <param name="rightButton">Whether the event is a right-click.</param>
        /// <returns>
        /// True if the event originated from a container that belongs to the control; otherwise
        /// false.
        /// </returns>
        protected bool UpdateSelectionFromEventSource(
            object eventSource,
            bool select = true,
            bool rangeModifier = false,
            bool toggleModifier = false,
            bool rightButton = false)
        {
            var container = GetContainerFromEventSource(eventSource);

            if (container != null)
            {
                UpdateSelectionFromContainer(container, select, rangeModifier, toggleModifier, rightButton);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tries to get the container that was the source of an event.
        /// </summary>
        /// <param name="eventSource">The control that raised the event.</param>
        /// <returns>The container or null if the event did not originate in a container.</returns>
        protected TreeViewItem? GetContainerFromEventSource(object eventSource)
        {
            var item = ((Visual)eventSource).GetSelfAndVisualAncestors()
                .OfType<TreeViewItem>()
                .FirstOrDefault();

            return item?.TreeViewOwner == this ? item : null;
        }

        /// <summary>
        /// Called when a container raises the 
        /// <see cref="SelectingItemsControl.IsSelectedChangedEvent"/>.
        /// </summary>
        /// <param name="e">The event.</param>
        private void ContainerSelectionChanged(RoutedEventArgs e)
        {
            if (e.Source is TreeViewItem container &&
                container.TreeViewOwner == this &&
                TreeItemFromContainer(container) is object item)
            {
                var containerIsSelected = SelectingItemsControl.GetIsSelected(container);
                var ourIsSelected = SelectedItems.Contains(item);

                if (containerIsSelected != ourIsSelected)
                {
                    if (containerIsSelected)
                        SelectedItems.Add(item);
                    else
                        SelectedItems.Remove(item);
                }
            }

            if (e.Source != this)
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Sets a container's 'selected' class or <see cref="ISelectable.IsSelected"/>.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="selected">Whether the control is selected</param>
        private void MarkContainerSelected(Control container, bool selected)
        {
            container.SetCurrentValue(SelectingItemsControl.IsSelectedProperty, selected);
        }

        /// <summary>
        /// Makes a list of objects equal another (though doesn't preserve order).
        /// </summary>
        /// <param name="items">The items collection.</param>
        /// <param name="desired">The desired items.</param>
        private static void SynchronizeItems(IList items, IEnumerable<object> desired)
        {
            var list = items.Cast<object>();
            var toRemove = list.Except(desired).ToList();
            var toAdd = desired.Except(list).ToList();

            foreach (var i in toRemove)
            {
                items.Remove(i);
            }

            foreach (var i in toAdd)
            {
                items.Add(i);
            }
        }
    }
}
