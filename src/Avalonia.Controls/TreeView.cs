
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Utils;
using Avalonia.Data;
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
        /// Defines the <see cref="Selection"/> property.
        /// </summary>
        public static readonly DirectProperty<TreeView, ISelectionModel> SelectionProperty =
            SelectingItemsControl.SelectionProperty.AddOwner<TreeView>(
                o => o.Selection,
                (o, v) => o.Selection = v);

        /// <summary>
        /// Defines the <see cref="SelectionMode"/> property.
        /// </summary>
        public static readonly StyledProperty<SelectionMode> SelectionModeProperty =
            ListBox.SelectionModeProperty.AddOwner<TreeView>();

        /// <summary>
        /// Defines the <see cref="SelectionChanged"/> property.
        /// </summary>
        public static RoutedEvent<SelectionChangedEventArgs> SelectionChangedEvent =
            SelectingItemsControl.SelectionChangedEvent;

        private object _selectedItem;
        private ISelectionModel _selection;
        private readonly SelectedItemsSync _selectedItems;

        /// <summary>
        /// Initializes static members of the <see cref="TreeView"/> class.
        /// </summary>
        static TreeView()
        {
            // HACK: Needed or SelectedItem property will not be found in Release build.
        }

        public TreeView()
        {
            // Setting Selection to null causes a default SelectionModel to be created.
            Selection = null;
            _selectedItems = new SelectedItemsSync(Selection);
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
        /// <summary>
        /// Gets or sets the selected item.
        /// </summary>
        public object SelectedItem
        {
            get => Selection.SelectedItem;
            set => Selection.SelectedIndex = IndexFromItem(value);
        }

        /// <summary>
        /// Gets or sets the selected items.
        /// </summary>
        protected IList SelectedItems
        {
            get => _selectedItems.GetOrCreateItems();
            set => _selectedItems.SetItems(value);
        }

        /// <summary>
        /// Gets or sets a model holding the current selection.
        /// </summary>
        public ISelectionModel Selection
        {
            get => _selection;
            set
            {
                value ??= new SelectionModel
                {
                    SingleSelect = !SelectionMode.HasFlagCustom(SelectionMode.Multiple),
                    AutoSelect = SelectionMode.HasFlagCustom(SelectionMode.AlwaysSelected),
                    RetainSelectionOnReset = true,
                };

                if (_selection != value)
                {
                    if (value == null)
                    {
                        throw new ArgumentNullException(nameof(value), "Cannot set Selection to null.");
                    }
                    else if (value.Source != null && value.Source != Items)
                    {
                        throw new ArgumentException("Selection has invalid Source.");
                    }

                    List<object> oldSelection = null;

                    if (_selection != null)
                    {
                        oldSelection = Selection.SelectedItems.ToList();
                        _selection.PropertyChanged -= OnSelectionModelPropertyChanged;
                        _selection.SelectionChanged -= OnSelectionModelSelectionChanged;
                        _selection.ChildrenRequested -= OnSelectionModelChildrenRequested;
                        MarkContainersUnselected();
                    }

                    _selection = value;

                    if (_selection != null)
                    {
                        _selection.Source = Items;
                        _selection.PropertyChanged += OnSelectionModelPropertyChanged;
                        _selection.SelectionChanged += OnSelectionModelSelectionChanged;
                        _selection.ChildrenRequested += OnSelectionModelChildrenRequested;

                        if (_selection.SingleSelect)
                        {
                            SelectionMode &= ~SelectionMode.Multiple;
                        }
                        else
                        {
                            SelectionMode |= SelectionMode.Multiple;
                        }

                        if (_selection.AutoSelect)
                        {
                            SelectionMode |= SelectionMode.AlwaysSelected;
                        }
                        else
                        {
                            SelectionMode &= ~SelectionMode.AlwaysSelected;
                        }

                        UpdateContainerSelection();

                        var selectedItem = SelectedItem;

                        if (_selectedItem != selectedItem)
                        {
                            RaisePropertyChanged(SelectedItemProperty, _selectedItem, selectedItem);
                            _selectedItem = selectedItem;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Expands the specified <see cref="TreeViewItem"/> all descendent <see cref="TreeViewItem"/>s.
        /// </summary>
        /// <param name="item">The item to expand.</param>
        public void ExpandSubTree(TreeViewItem item)
        {
            item.IsExpanded = true;

            var panel = item.Presenter.Panel;

            if (panel != null)
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
        /// Selects all items in the <see cref="TreeView"/>.
        /// </summary>
        /// <remarks>
        /// Note that this method only selects nodes currently visible due to their parent nodes
        /// being expanded: it does not expand nodes.
        /// </remarks>
        public void SelectAll() => Selection.SelectAll();

        /// <summary>
        /// Deselects all items in the <see cref="TreeView"/>.
        /// </summary>
        public void UnselectAll() => Selection.ClearSelection();

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
                TreeViewItem.IsExpandedProperty);
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
                    (e.KeyModifiers & KeyModifiers.Shift) != 0);
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
                        FocusManager.Instance.Focus(next, NavigationMethod.Directional);
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
                    SelectAll();
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// Called when <see cref="SelectionModel.PropertyChanged"/> is raised.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void OnSelectionModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectionModel.AnchorIndex) && AutoScrollToSelectedItem)
            {
                var container = ContainerFromIndex(Selection.AnchorIndex);

                if (container != null)
                {
                    DispatcherTimer.RunOnce(container.BringIntoView, TimeSpan.Zero);
                }
            }
        }

        /// <summary>
        /// Called when <see cref="SelectionModel.SelectionChanged"/> is raised.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void OnSelectionModelSelectionChanged(object sender, SelectionModelSelectionChangedEventArgs e)
        {
            void Mark(IndexPath index, bool selected)
            {
                var container = ContainerFromIndex(index);

                if (container != null)
                {
                    MarkContainerSelected(container, selected);
                }
            }

            foreach (var i in e.SelectedIndices)
            {
                Mark(i, true);
            }

            foreach (var i in e.DeselectedIndices)
            {
                Mark(i, false);
            }

            var newSelectedItem = SelectedItem;

            if (newSelectedItem != _selectedItem)
            {
                RaisePropertyChanged(SelectedItemProperty, _selectedItem, newSelectedItem);
                _selectedItem = newSelectedItem;
            }

            var ev = new SelectionChangedEventArgs(
                SelectionChangedEvent,
                e.DeselectedItems.ToList(),
                e.SelectedItems.ToList());
            RaiseEvent(ev);
        }

        private void OnSelectionModelChildrenRequested(object sender, SelectionModelChildrenRequestedEventArgs e)
        {
            var container = ItemContainerGenerator.Index.ContainerFromItem(e.Source) as TreeViewItem;

            if (container is object)
            {
                if (e.SourceIndex.IsAncestorOf(e.FinalIndex))
                {
                    container.IsExpanded = true;
                    container.ApplyTemplate();
                    container.Presenter?.ApplyTemplate();
                }

                e.Children = Observable.CombineLatest(
                    container.GetObservable(TreeViewItem.IsExpandedProperty),
                    container.GetObservable(ItemsProperty),
                    (expanded, items) => expanded ? items : null);
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
                        result = previous.IsExpanded && previous.ItemCount > 0 ?
                            (TreeViewItem)previous.ItemContainerGenerator.ContainerFromIndex(previous.ItemCount - 1) :
                            previous;
                    }
                    else
                    {
                        result = from.Parent as TreeViewItem;
                    }

                    break;

                case NavigationDirection.Down:
                    if (from.IsExpanded && intoChildren && from.ItemCount > 0)
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

        protected override void ItemsChanged(AvaloniaPropertyChangedEventArgs e)
        {
            Selection.Source = Items;
            base.ItemsChanged(e);
        }

        /// <inheritdoc/>
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            if (e.Source is IVisual source)
            {
                var point = e.GetCurrentPoint(source);

                if (point.Properties.IsLeftButtonPressed || point.Properties.IsRightButtonPressed)
                {
                    e.Handled = UpdateSelectionFromEventSource(
                        e.Source,
                        true,
                        (e.KeyModifiers & KeyModifiers.Shift) != 0,
                        (e.KeyModifiers & KeyModifiers.Control) != 0,
                        point.Properties.IsRightButtonPressed);
                }
            }
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == SelectionModeProperty)
            {
                var mode = change.NewValue.GetValueOrDefault<SelectionMode>();
                Selection.SingleSelect = !mode.HasFlagCustom(SelectionMode.Multiple);
                Selection.AutoSelect = mode.HasFlagCustom(SelectionMode.AlwaysSelected);
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
            IControl container,
            bool select = true,
            bool rangeModifier = false,
            bool toggleModifier = false,
            bool rightButton = false)
        {
            var index = IndexFromContainer((TreeViewItem)container);

            if (index.GetSize() == 0)
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

            if (!select)
            {
                Selection.DeselectAt(index);
            }
            else if (rightButton)
            {
                if (!Selection.IsSelectedAt(index))
                {
                    Selection.SelectedIndex = index;
                }
            }
            else if (!toggle && !range)
            {
                Selection.SelectedIndex = index;
            }
            else if (multi && range)
            {
                using var operation = Selection.Update();
                var anchor = Selection.AnchorIndex;

                if (anchor.GetSize() == 0)
                {
                    anchor = new IndexPath(0);
                }

                Selection.ClearSelection();
                Selection.AnchorIndex = anchor;
                Selection.SelectRangeFromAnchorTo(index);
            }
            else
            {
                if (Selection.IsSelectedAt(index))
                {
                    Selection.DeselectAt(index);
                }
                else if (multi)
                {
                    Selection.SelectAt(index);
                }
                else
                {
                    Selection.SelectedIndex = index;
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

        private void MarkContainersUnselected()
        {
            foreach (var container in ItemContainerGenerator.Index.Containers)
            {
                MarkContainerSelected(container, false);
            }
        }

        private void UpdateContainerSelection()
        {
            var index = ItemContainerGenerator.Index;

            foreach (var container in index.Containers)
            {
                var i = IndexFromContainer((TreeViewItem)container);

                MarkContainerSelected(
                    container,
                    Selection.IsSelectedAt(i) != false);
            }
        }

        private static IndexPath IndexFromContainer(TreeViewItem container)
        {
            var result = new List<int>();

            while (true)
            {
                if (container.Level == 0)
                {
                    var treeView = container.FindAncestorOfType<TreeView>();

                    if (treeView == null)
                    {
                        return default;
                    }

                    result.Add(treeView.ItemContainerGenerator.IndexFromContainer(container));
                    result.Reverse();
                    return new IndexPath(result);
                }
                else
                {
                    var parent = container.FindAncestorOfType<TreeViewItem>();

                    if (parent == null)
                    {
                        return default;
                    }

                    result.Add(parent.ItemContainerGenerator.IndexFromContainer(container));
                    container = parent;
                }
            }
        }

        private IndexPath IndexFromItem(object item)
        {
            var container = ItemContainerGenerator.Index.ContainerFromItem(item) as TreeViewItem;

            if (container != null)
            {
                return IndexFromContainer(container);
            }

            return default;
        }

        private TreeViewItem ContainerFromIndex(IndexPath index)
        {
            TreeViewItem treeViewItem = null;

            for (var i = 0; i < index.GetSize(); ++i)
            {
                var generator = treeViewItem?.ItemContainerGenerator ?? ItemContainerGenerator;
                treeViewItem = generator.ContainerFromIndex(index.GetAt(i)) as TreeViewItem;

                if (treeViewItem == null)
                {
                    return null;
                }
            }

            return treeViewItem;
        }
    }
}
