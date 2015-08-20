// -----------------------------------------------------------------------
// <copyright file="SelectingItemsControl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Primitives
{
    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Linq;
    using Perspex.Controls.Generators;
    using Perspex.Input;
    using Perspex.Interactivity;
    using Perspex.Styling;
    using Perspex.VisualTree;

    /// <summary>
    /// An <see cref="ItemsControl"/> that maintains a selection.
    /// </summary>
    /// <remarks>
    /// TODO: Support multiple selection.
    /// </remarks>
    public class SelectingItemsControl : ItemsControl
    {
        /// <summary>
        /// Defines the <see cref="AutoSelect"/> property.
        /// </summary>
        public static readonly PerspexProperty<bool> AutoSelectProperty =
            PerspexProperty.Register<SelectingItemsControl, bool>("AutoSelect");

        /// <summary>
        /// Defines the <see cref="SelectedIndex"/> property.
        /// </summary>
        public static readonly PerspexProperty<int> SelectedIndexProperty =
            PerspexProperty.Register<SelectingItemsControl, int>(
                nameof(SelectedIndex),
                defaultValue: -1,
                validate: ValidateSelectedIndex);

        /// <summary>
        /// Defines the <see cref="SelectedItem"/> property.
        /// </summary>
        public static readonly PerspexProperty<object> SelectedItemProperty =
            PerspexProperty.Register<SelectingItemsControl, object>(
                nameof(SelectedItem),
                validate: ValidateSelectedItem);

        /// <summary>
        /// Event that should be raised by items that implement <see cref="ISelectable"/> to
        /// notify the parent <see cref="SelectingItemsControl"/> that their selection state
        /// has changed.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> IsSelectedChangedEvent =
            RoutedEvent.Register<SelectingItemsControl, RoutedEventArgs>("IsSelectedChanged", RoutingStrategies.Bubble);

        /// <summary>
        /// Initializes static members of the <see cref="SelectingItemsControl"/> class.
        /// </summary>
        static SelectingItemsControl()
        {
            IsSelectedChangedEvent.AddClassHandler<SelectingItemsControl>(x => x.ContainerSelectionChanged);
            SelectedIndexProperty.Changed.AddClassHandler<SelectingItemsControl>(x => x.SelectedIndexChanged);
            SelectedItemProperty.Changed.AddClassHandler<SelectingItemsControl>(x => x.SelectedItemChanged);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectingItemsControl"/> class.
        /// </summary>
        public SelectingItemsControl()
        {
            this.ItemContainerGenerator.ContainersInitialized.Subscribe(this.ContainersInitialized);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the control should always try to keep an item
        /// selected where possible.
        /// </summary>
        public bool AutoSelect
        {
            get { return this.GetValue(AutoSelectProperty); }
            set { this.SetValue(AutoSelectProperty, value); }
        }

        /// <summary>
        /// Gets or sets the index of the selected item.
        /// </summary>
        public int SelectedIndex
        {
            get { return this.GetValue(SelectedIndexProperty); }
            set { this.SetValue(SelectedIndexProperty, value); }
        }

        /// <summary>
        /// Gets or sets the selected item.
        /// </summary>
        public object SelectedItem
        {
            get { return this.GetValue(SelectedItemProperty); }
            set { this.SetValue(SelectedItemProperty, value); }
        }

        /// <inheritdoc/>
        protected override void ItemsChanged(PerspexPropertyChangedEventArgs e)
        {
            base.ItemsChanged(e);

            if (this.SelectedIndex != -1)
            {
                this.SelectedIndex = IndexOf((IEnumerable)e.NewValue, this.SelectedItem);
            }
            else if (this.AutoSelect && this.Items != null & this.Items.Cast<object>().Any())
            {
                this.SelectedIndex = 0;
            }
        }

        /// <inheritdoc/>
        protected override void ItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            base.ItemsCollectionChanged(sender, e);

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (this.AutoSelect && this.SelectedIndex == -1)
                    {
                        this.SelectedIndex = 0;
                    }

                    break;

                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Replace:
                    var selectedIndex = this.SelectedIndex;

                    if (selectedIndex >= e.OldStartingIndex &&
                        selectedIndex < e.OldStartingIndex + e.OldItems.Count)
                    {
                        if (!this.AutoSelect)
                        {
                            this.SelectedIndex = -1;
                        }
                        else
                        {
                            this.LostSelection();
                        }
                    }

                    break;

                case NotifyCollectionChangedAction.Reset:
                    this.SelectedIndex = IndexOf(e.NewItems, this.SelectedItem);
                    break;
            }
        }

        /// <inheritdoc/>
        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);

            if (e.NavigationMethod == NavigationMethod.Pointer ||
                e.NavigationMethod == NavigationMethod.Directional)
            {
                this.TrySetSelectionFromContainerEvent(e.Source, true);
            }
        }

        /// <inheritdoc/>
        protected override void OnPointerPressed(PointerPressEventArgs e)
        {
            base.OnPointerPressed(e);
            e.Handled = true;
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
                        if (object.Equals(i, item))
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
        private static void MarkContainerSelected(IControl container, bool selected)
        {
            var selectable = container as ISelectable;
            var styleable = container as IStyleable;

            if (selectable != null)
            {
                selectable.IsSelected = selected;
            }
            else if (styleable != null)
            {
                if (selected)
                {
                    styleable.Classes.Add("selected");
                }
                else
                {
                    styleable.Classes.Remove("selected");
                }
            }
        }

        /// <summary>
        /// Coerces the <see cref="SelectedIndex"/> property.
        /// </summary>
        /// <param name="sender">The object with the property.</param>
        /// <param name="index">The proposed value of the property.</param>
        /// <returns>The final value of the property.</returns>
        private static int ValidateSelectedIndex(SelectingItemsControl sender, int index)
        {
            var items = sender.Items;
            return (index >= 0 && index < items?.Cast<object>().Count()) ? index : -1;
        }

        /// <summary>
        /// Coerces the <see cref="SelectedItem"/> property.
        /// </summary>
        /// <param name="sender">The object with the property.</param>
        /// <param name="item">The proposed value of the property.</param>
        /// <returns>The final value of the property.</returns>
        private static object ValidateSelectedItem(SelectingItemsControl sender, object item)
        {
            var items = sender.Items;
            return items?.Cast<object>().Contains(item) == true ? item : null;
        }

        /// <summary>
        /// Called when new containers are initialized by the <see cref="ItemContainerGenerator"/>.
        /// </summary>
        /// <param name="containers">The containers.</param>
        private void ContainersInitialized(ItemContainers containers)
        {
            var selectedIndex = this.SelectedIndex;
            var selectedContainer = containers.Items.OfType<ISelectable>().FirstOrDefault(x => x.IsSelected);

            if (selectedContainer != null)
            {
                this.SelectedIndex = containers.Items.IndexOf((IControl)selectedContainer) + containers.StartingIndex;
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
            var selectable = (ISelectable)e.Source;

            if (selectable != null)
            {
                this.TrySetSelectionFromContainerEvent(e.Source, selectable.IsSelected);
            }
        }

        /// <summary>
        /// Called when the <see cref="SelectedIndex"/> property changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void SelectedIndexChanged(PerspexPropertyChangedEventArgs e)
        {
            var index = (int)e.OldValue;

            if (index != -1)
            {
                var container = this.ItemContainerGenerator.ContainerFromIndex(index);
                MarkContainerSelected(container, false);
            }

            index = (int)e.NewValue;

            if (index == -1)
            {
                this.SelectedItem = null;
            }
            else
            {
                this.SelectedItem = this.Items.Cast<object>().ElementAt((int)e.NewValue);
                var container = this.ItemContainerGenerator.ContainerFromIndex(index);
                MarkContainerSelected(container, true);

                var inputElement = container as IInputElement;
                if (inputElement != null && this.Presenter != null && this.Presenter.Panel != null)
                {
                    KeyboardNavigation.SetTabOnceActiveElement(this.Presenter.Panel, inputElement);
                }
            }
        }

        /// <summary>
        /// Called when the <see cref="SelectedItem"/> property changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void SelectedItemChanged(PerspexPropertyChangedEventArgs e)
        {
            this.SelectedIndex = IndexOf(this.Items, e.NewValue);
        }

        /// <summary>
        /// Tries to get the container that was the source of an event.
        /// </summary>
        /// <param name="eventSource">The control that raised the event.</param>
        /// <returns>The container or null if the event did not originate in a container.</returns>
        private IControl GetContainerFromEvent(IInteractive eventSource)
        {
            var item = ((IVisual)eventSource).GetSelfAndVisualAncestors()
                .OfType<ILogical>()
                .FirstOrDefault(x => x.LogicalParent == this);

            return item as IControl;
        }

        /// <summary>
        /// Called when the currently selected item is lost and the selection must be changed
        /// depending on the <see cref="AutoSelect"/> property.
        /// </summary>
        private void LostSelection()
        {
            var items = this.Items?.Cast<object>();

            if (items != null && this.AutoSelect)
            {
                var index = Math.Min(this.SelectedIndex, items.Count() - 1);

                if (index > -1)
                {
                    this.SelectedItem = items.ElementAt(index);
                    return;
                }
            }

            this.SelectedIndex = -1;
        }

        /// <summary>
        /// Tries to set the selection to a container that raised an event.
        /// </summary>
        /// <param name="eventSource">The control that raised the event.</param>
        /// <param name="select">Whether the container should be selected or unselected.</param>
        private void TrySetSelectionFromContainerEvent(IInteractive eventSource, bool select)
        {
            var item = this.GetContainerFromEvent(eventSource);

            if (item != null)
            {
                var index = this.ItemContainerGenerator.IndexFromContainer(item);

                if (index != -1)
                {
                    if (select)
                    {
                        this.SelectedIndex = index;
                    }
                    else
                    {
                        this.LostSelection();
                    }
                }
            }
        }
    }
}
