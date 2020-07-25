using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Utils;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
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
    /// current multiple <see cref="Selection"/> and <see cref="SelectedItems"/> together with the
    /// <see cref="SelectionMode"/> and properties are protected, however a derived  class can
    /// expose these if it wishes to support multiple selection.
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
        /// Defines the <see cref="Selection"/> property.
        /// </summary>
        public static readonly DirectProperty<SelectingItemsControl, ISelectionModel> SelectionProperty =
            AvaloniaProperty.RegisterDirect<SelectingItemsControl, ISelectionModel>(
                nameof(Selection),
                o => o.Selection,
                (o, v) => o.Selection = v);

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
        private readonly SelectedItemsSync _selectedItems;
        private ISelectionModel _selection;
        private int _selectedIndex = -1;
        private object _selectedItem;
        private bool _ignoreContainerSelectionChanged;
        private int _updateCount;
        private int _updateSelectedIndex;
        private object _updateSelectedItem;

        public SelectingItemsControl()
        {
            // Setting Selection to null causes a default SelectionModel to be created.
            Selection = null;
            _selectedItems = new SelectedItemsSync(Selection);
        }

        /// <summary>
        /// Initializes static members of the <see cref="SelectingItemsControl"/> class.
        /// </summary>
        static SelectingItemsControl()
        {
            IsSelectedChangedEvent.AddClassHandler<SelectingItemsControl>((x, e) => x.ContainerSelectionChanged(e));
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
            get => Selection.SelectedIndex != default ? Selection.SelectedIndex.GetAt(0) : -1;
            set
            {
                if (_updateCount == 0)
                {
                    if (value != SelectedIndex)
                    {
                        Selection.SelectedIndex = new IndexPath(value);
                    }
                }
                else
                {
                    _updateSelectedIndex = value;
                    _updateSelectedItem = null;
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected item.
        /// </summary>
        public object SelectedItem
        {
            get => Selection.SelectedItem;
            set
            {
                if (_updateCount == 0)
                {
                    SelectedIndex = IndexOf(Items, value);
                }
                else
                {
                    _updateSelectedItem = value;
                    _updateSelectedIndex = int.MinValue;
                }
            }
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
        protected ISelectionModel Selection 
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
                        MarkContainersUnselected();
                    }

                    _selection = value;

                    if (oldSelection?.Count > 0)
                    {
                        RaiseEvent(new SelectionChangedEventArgs(
                            SelectionChangedEvent,
                            oldSelection,
                            Array.Empty<object>()));
                    }

                    if (_selection != null)
                    {
                        _selection.Source = Items;
                        _selection.PropertyChanged += OnSelectionModelPropertyChanged;
                        _selection.SelectionChanged += OnSelectionModelSelectionChanged;

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

                        var selectedIndex = SelectedIndex;
                        var selectedItem = SelectedItem;

                        if (_selectedIndex != selectedIndex)
                        {
                            RaisePropertyChanged(SelectedIndexProperty, _selectedIndex, selectedIndex);
                            _selectedIndex = selectedIndex;
                        }

                        if (_selectedItem != selectedItem)
                        {
                            RaisePropertyChanged(SelectedItemProperty, _selectedItem, selectedItem);
                            _selectedItem = selectedItem;
                        }
                        
                        if (selectedIndex != -1)
                        {
                            RaiseEvent(new SelectionChangedEventArgs(
                                SelectionChangedEvent,
                                Array.Empty<object>(),
                                Selection.SelectedItems.ToList()));
                        }
                    }
                }
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

            InternalBeginInit();
        }

        /// <inheritdoc/>
        public override void EndInit()
        {
            InternalEndInit();

            base.EndInit();
        }

        /// <summary>
        /// Scrolls the specified item into view.
        /// </summary>
        /// <param name="index">The index of the item.</param>
        public void ScrollIntoView(int index) => Presenter?.ScrollIntoView(index);

        /// <summary>
        /// Scrolls the specified item into view.
        /// </summary>
        /// <param name="item">The item.</param>
        public void ScrollIntoView(object item) => ScrollIntoView(IndexOf(Items, item));

        /// <summary>
        /// Tries to get the container that was the source of an event.
        /// </summary>
        /// <param name="eventSource">The control that raised the event.</param>
        /// <returns>The container or null if the event did not originate in a container.</returns>
        protected IControl GetContainerFromEventSource(IInteractive eventSource)
        {
            var parent = (IVisual)eventSource;

            while (parent != null)
            {
                if (parent is IControl control && control.LogicalParent == this
                                               && ItemContainerGenerator?.IndexFromContainer(control) != -1)
                {
                    return control;
                }

                parent = parent.VisualParent;
            }

            return null;
        }

        /// <inheritdoc/>
        protected override void ItemsChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (_updateCount == 0)
            {
                Selection.Source = e.NewValue;
            }

            base.ItemsChanged(e);
        }

        /// <inheritdoc/>
        protected override void ItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            base.ItemsCollectionChanged(sender, e);
        }

        /// <inheritdoc/>
        protected override void OnContainersMaterialized(ItemContainerEventArgs e)
        {
            base.OnContainersMaterialized(e);

            foreach (var container in e.Containers)
            {
                if ((container.ContainerControl as ISelectable)?.IsSelected == true)
                {
                    Selection.Select(container.Index);
                    MarkContainerSelected(container.ContainerControl, true);
                }
                else if (Selection.IsSelected(container.Index) == true)
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
                    bool selected = Selection.IsSelected(i.Index) == true;
                    MarkContainerSelected(i.ContainerControl, selected);
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnDataContextBeginUpdate()
        {
            base.OnDataContextBeginUpdate();

            InternalBeginInit();
        }

        /// <inheritdoc/>
        protected override void OnDataContextEndUpdate()
        {
            base.OnDataContextEndUpdate();

            InternalEndInit();
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
                    Selection.SelectAll();
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
                        if (Selection.IsSelected(index) == false)
                        {
                            SelectedIndex = index;
                        }
                    }
                    else if (range)
                    {
                        using var operation = Selection.Update();
                        var anchor = Selection.AnchorIndex;

                        if (anchor.GetSize() == 0)
                        {
                            anchor = new IndexPath(0);
                        }

                        Selection.ClearSelection();
                        Selection.AnchorIndex = anchor;
                        Selection.SelectRangeFromAnchor(index);
                    }
                    else if (multi && toggle)
                    {
                        if (Selection.IsSelected(index) == true)
                        {
                            Selection.Deselect(index);
                        }
                        else
                        {
                            Selection.Select(index);
                        }
                    }
                    else if (toggle)
                    {
                        SelectedIndex = (SelectedIndex == index) ? -1 : index;
                    }
                    else
                    {
                        using var operation = Selection.Update();
                        Selection.ClearSelection();
                        Selection.Select(index);
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
        /// Called when <see cref="SelectionModel.PropertyChanged"/> is raised.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void OnSelectionModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectionModel.AnchorIndex) && AutoScrollToSelectedItem)
            {
                if (Selection.AnchorIndex.GetSize() > 0)
                {
                    ScrollIntoView(Selection.AnchorIndex.GetAt(0));
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
            void Mark(int index, bool selected)
            {
                var container = ItemContainerGenerator.ContainerFromIndex(index);

                if (container != null)
                {
                    MarkContainerSelected(container, selected);
                }
            }

            if (e.SelectedIndices.Count > 0 || e.DeselectedIndices.Count > 0)
            {
                foreach (var i in e.SelectedIndices)
                {
                    Mark(i.GetAt(0), true);
                }

                foreach (var i in e.DeselectedIndices)
                {
                    Mark(i.GetAt(0), false);
                }
            }
            else if (e.DeselectedItems.Count > 0)
            {
                // (De)selected indices being empty means that a selected item was removed from
                // the Items (it can't tell us the index of the item because the index is no longer
                // valid). In this case, we just update the selection state of all containers.
                UpdateContainerSelection();
            }

            var newSelectedIndex = SelectedIndex;
            var newSelectedItem = SelectedItem;

            if (newSelectedIndex != _selectedIndex)
            {
                RaisePropertyChanged(SelectedIndexProperty, _selectedIndex, newSelectedIndex);
                _selectedIndex = newSelectedIndex;
            }

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

        private void MarkContainersUnselected()
        {
            foreach (var container in ItemContainerGenerator.Containers)
            {
                MarkContainerSelected(container.ContainerControl, false);
            }
        }

        private void UpdateContainerSelection()
        {
            foreach (var container in ItemContainerGenerator.Containers)
            {
                MarkContainerSelected(
                    container.ContainerControl,
                    Selection.IsSelected(container.Index) != false);
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

        private void UpdateFinished()
        {
            Selection.Source = Items;

            if (_updateSelectedItem != null)
            {
                SelectedItem = _updateSelectedItem;
            }
            else
            {
                if (ItemCount == 0 && SelectedIndex != -1)
                {
                    SelectedIndex = -1;
                }
                else
                {
                    if (_updateSelectedIndex != int.MinValue)
                    {
                        SelectedIndex = _updateSelectedIndex;
                    }

                    if (AlwaysSelected && SelectedIndex == -1)
                    {
                        SelectedIndex = 0;
                    }
                }
            }
        }

        private void InternalBeginInit()
        {
            if (_updateCount == 0)
            {
                _updateSelectedIndex = int.MinValue;
            }

            ++_updateCount;
        }

        private void InternalEndInit()
        {
            Debug.Assert(_updateCount > 0);

            if (--_updateCount == 0)
            {
                UpdateFinished();
            }
        }
    }
}
