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
    using Perspex.Controls.Utils;
    using Perspex.Input;
    using Perspex.Interactivity;
    using Perspex.VisualTree;

    /// <summary>
    /// An <see cref="ItemsControl"/> that maintains a selection.
    /// </summary>
    /// <remarks>
    /// TODO: Support multiple selection.
    /// </remarks>
    public abstract class SelectingItemsControl : ItemsControl
    {
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
            IsSelectedChangedEvent.AddClassHandler<SelectingItemsControl>(x => x.ItemIsSelectedChanged);
            SelectedIndexProperty.Changed.Subscribe(SelectedIndexChanged);
            SelectedItemProperty.Changed.Subscribe(SelectedItemChanged);
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

        /// <summary>
        /// Called when the <see cref="Items"/> property changes.
        /// </summary>
        /// <param name="oldValue">The old value of the property.</param>
        /// <param name="newValue">The new value of the property.</param>
        protected override void ItemsChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            base.ItemsChanged(oldValue, newValue);

            var selected = this.SelectedItem;

            if (selected != null)
            {
                if (newValue == null || !newValue.Contains(selected))
                {
                    this.SelectedItem = null;
                }
            }
        }

        /// <summary>
        /// Called when a <see cref="INotifyCollectionChanged.CollectionChanged"/> event is raised
        /// on <see cref="Items"/>.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        protected override void ItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            base.ItemsCollectionChanged(sender, e);

            var selected = this.SelectedItem;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Reset:
                    if (e.OldItems.Contains(selected))
                    {
                        this.SelectedItem = null;
                    }

                    break;
                case NotifyCollectionChangedAction.Move:
                    this.SelectedItem = this.Items.IndexOf(selected);
                    break;
            }
        }

        /// <summary>
        /// Called when the selection on a child item changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void ItemIsSelectedChanged(RoutedEventArgs e)
        {
            var selectable = e.Source as ISelectable;

            if (selectable != null && selectable != this && selectable.IsSelected)
            {
                var container = this.ItemContainerGenerator.GetItemForContainer((Control)selectable);

                if (container != null)
                {
                    this.SelectedItem = container;
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// Moves the selection in the specified direction.
        /// </summary>
        /// <param name="direction">The direction.</param>
        protected virtual void MoveSelection(FocusNavigationDirection direction)
        {
            var panel = this.Presenter?.Panel as INavigablePanel;
            var selected = this.SelectedItem;
            var container = selected != null ?
                this.ItemContainerGenerator.GetContainerForItem(selected) :
                null;

            if (panel != null)
            {
                var next = panel.GetControl(direction, container);

                if (next != null)
                {
                    this.SelectedItem = this.ItemContainerGenerator.GetItemForContainer(next);
                }
            }
            else
            {
                // TODO: Try doing a visual search?
            }
        }

        /// <summary>
        /// Called when a key is pressed within the control.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (!e.Handled)
            {
                switch (e.Key)
                {
                    case Key.Up:
                        this.MoveSelection(FocusNavigationDirection.Up);
                        break;
                    case Key.Down:
                        this.MoveSelection(FocusNavigationDirection.Down);
                        break;
                    case Key.Left:
                        this.MoveSelection(FocusNavigationDirection.Left);
                        break;
                    case Key.Right:
                        this.MoveSelection(FocusNavigationDirection.Right);
                        break;
                    default:
                        return;
                }

                var selected = this.SelectedItem;

                if (selected != null)
                {
                    var container = this.ItemContainerGenerator.GetContainerForItem(selected);

                    if (container != null)
                    {
                        container.BringIntoView();
                        FocusManager.Instance.Focus(container, true);
                    }
                }

                e.Handled = true;
            }
        }

        /// <summary>
        /// Called when the pointer is pressed within the control.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected override void OnPointerPressed(PointerPressEventArgs e)
        {
            IVisual source = (IVisual)e.Source;
            var selectable = source.GetVisualAncestors()
                .OfType<ISelectable>()
                .OfType<Control>()
                .FirstOrDefault();

            if (selectable != null)
            {
                var item = this.ItemContainerGenerator.GetItemForContainer(selectable);

                if (item != null)
                {
                    this.SelectedItem = item;
                    selectable.BringIntoView();
                    FocusManager.Instance.Focus(selectable);
                }
            }

            e.Handled = true;
        }

        /// <summary>
        /// Called when the control's template has been applied.
        /// </summary>
        protected override void OnTemplateApplied()
        {
            base.OnTemplateApplied();
            this.SelectedItemChanged(this.SelectedItem);
        }

        /// <summary>
        /// Provides coercion for the <see cref="SelectedIndex"/> property.
        /// </summary>
        /// <param name="o">The object on which the property has changed.</param>
        /// <param name="value">The proposed value.</param>
        /// <returns>The coerced value.</returns>
        private static int ValidateSelectedIndex(PerspexObject o, int value)
        {
            var control = o as SelectingItemsControl;

            if (control != null)
            {
                if (value < -1)
                {
                    return -1;
                }
                else if (value > -1)
                {
                    var items = control.Items;

                    if (items != null)
                    {
                        var count = items.Count();
                        return Math.Min(value, count - 1);
                    }
                    else
                    {
                        return -1;
                    }
                }
            }

            return value;
        }

        /// <summary>
        /// Provides coercion for the <see cref="SelectedItem"/> property.
        /// </summary>
        /// <param name="o">The object on which the property has changed.</param>
        /// <param name="value">The proposed value.</param>
        /// <returns>The coerced value.</returns>
        private static object ValidateSelectedItem(PerspexObject o, object value)
        {
            var control = o as SelectingItemsControl;

            if (control != null)
            {
                if (value != null && (control.Items == null || control.Items.IndexOf(value) == -1))
                {
                    return null;
                }
            }

            return value;
        }

        /// <summary>
        /// Called when the <see cref="SelectedIndex"/> property changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        private static void SelectedIndexChanged(PerspexPropertyChangedEventArgs e)
        {
            var control = e.Sender as SelectingItemsControl;

            if (control != null)
            {
                var index = (int)e.NewValue;

                if (index == -1)
                {
                    control.SelectedItem = null;
                }
                else
                {
                    control.SelectedItem = control.Items.ElementAt((int)e.NewValue);
                }
            }
        }

        /// <summary>
        /// Called when the <see cref="SelectedItem"/> property changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        private static void SelectedItemChanged(PerspexPropertyChangedEventArgs e)
        {
            var control = e.Sender as SelectingItemsControl;

            if (control != null)
            {
                control.SelectedItemChanged(e.NewValue);
            }
        }

        /// <summary>
        /// Called when the <see cref="SelectedItem"/> property changes.
        /// </summary>
        /// <param name="selected">The new selected item.</param>
        private void SelectedItemChanged(object selected)
        {
            var containers = this.ItemContainerGenerator.GetAll()
                .Select(x => x.Item2)
                .OfType<ISelectable>();
            var selectedContainer = (selected != null) ?
                this.ItemContainerGenerator.GetContainerForItem(selected) :
                null;

            if (this.Presenter != null && this.Presenter.Panel != null)
            {
                KeyboardNavigation.SetTabOnceActiveElement(this.Presenter.Panel, selectedContainer);
            }

            foreach (var item in containers)
            {
                item.IsSelected = item == selectedContainer;
            }

            if (selected == null)
            {
                this.SelectedIndex = -1;
            }
            else
            {
                var items = this.Items;

                if (items != null)
                {
                    this.SelectedIndex = items.IndexOf(selected);
                }
            }
        }
    }
}
