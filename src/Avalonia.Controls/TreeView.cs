// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
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
        /// Defines the <see cref="SelectedItemChanged"/> event.
        /// </summary>
        public static readonly RoutedEvent<SelectionChangedEventArgs> SelectedItemChangedEvent =
            RoutedEvent.Register<TreeView, SelectionChangedEventArgs>(
                "SelectedItemChanged",
                RoutingStrategies.Bubble);

        private object _selectedItem;

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
        public event EventHandler<SelectionChangedEventArgs> SelectedItemChanged
        {
            add { AddHandler(SelectedItemChangedEvent, value); }
            remove { RemoveHandler(SelectedItemChangedEvent, value); }
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
            get { return GetValue(AutoScrollToSelectedItemProperty); }
            set { SetValue(AutoScrollToSelectedItemProperty, value); }
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
                if (_selectedItem != null)
                {
                    var container = ItemContainerGenerator.Index.ContainerFromItem(_selectedItem);
                    MarkContainerSelected(container, false);
                }

                var oldItem = _selectedItem;
                SetAndRaise(SelectedItemProperty, ref _selectedItem, value);

                if (_selectedItem != null)
                {
                    var container = ItemContainerGenerator.Index.ContainerFromItem(_selectedItem);
                    MarkContainerSelected(container, true);

                    if (AutoScrollToSelectedItem && container != null)
                    {
                        container.BringIntoView();
                    }
                }

                if (oldItem != _selectedItem)
                {
                    // Fire the SelectionChanged event
                    List<object> removed = new List<object>();
                    if (oldItem != null)
                    {
                        removed.Add(oldItem);
                    }

                    List<object> added = new List<object>();
                    if (_selectedItem != null)
                    {
                        added.Add(_selectedItem);
                    }

                    var changed = new SelectionChangedEventArgs(
                        SelectedItemChangedEvent,
                        added,
                        removed);
                    RaiseEvent(changed);
                }
            }
        }

        (bool handled, IInputElement next) ICustomKeyboardNavigation.GetNext(IInputElement element, NavigationDirection direction)
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
                else
                {
                    return (true, null);
                }
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
                        GetContainerFromEventSource(e.Source) as TreeViewItem,
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
        }

        private TreeViewItem GetContainerInDirection(
            TreeViewItem from,
            NavigationDirection direction,
            bool intoChildren)
        {
            IItemContainerGenerator parentGenerator;

            if (from?.Parent is TreeView treeView)
            {
                parentGenerator = treeView.ItemContainerGenerator;
            }
            else if (from?.Parent is TreeViewItem item)
            {
                parentGenerator = item.ItemContainerGenerator;
            }
            else
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

            if (item != null)
            {
                if (SelectedItem != null)
                {
                    var old = ItemContainerGenerator.Index.ContainerFromItem(SelectedItem);
                    MarkContainerSelected(old, false);
                }

                SelectedItem = item;

                MarkContainerSelected(container, true);
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
        protected IControl GetContainerFromEventSource(IInteractive eventSource)
        {
            var item = ((IVisual)eventSource).GetSelfAndVisualAncestors()
                .OfType<TreeViewItem>()
                .FirstOrDefault();

            if (item != null)
            {
                if (item.ItemContainerGenerator.Index == this.ItemContainerGenerator.Index)
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

            if (selectedItem != null)
            {
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
        }

        /// <summary>
        /// Sets a container's 'selected' class or <see cref="ISelectable.IsSelected"/>.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="selected">Whether the control is selected</param>
        private void MarkContainerSelected(IControl container, bool selected)
        {
            if (container != null)
            {
                var selectable = container as ISelectable;

                if (selectable != null)
                {
                    selectable.IsSelected = selected;
                }
                else
                {
                    ((IPseudoClasses)container.Classes).Set(":selected", selected);
                }
            }
        }
    }
}
