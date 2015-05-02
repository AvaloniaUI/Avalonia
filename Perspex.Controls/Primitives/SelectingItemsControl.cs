// -----------------------------------------------------------------------
// <copyright file="SelectingItemsControl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Primitives
{
    using Perspex.Controls.Utils;
    using Perspex.Input;
    using Perspex.VisualTree;
    using System;
    using System.Linq;
    using System.Collections;
    using System.Collections.Specialized;

    public abstract class SelectingItemsControl : ItemsControl
    {
        public static readonly PerspexProperty<int> SelectedIndexProperty =
            PerspexProperty.Register<SelectingItemsControl, int>("SelectedIndex", coerce: CoerceSelectedIndex);

        public static readonly PerspexProperty<object> SelectedItemProperty =
            PerspexProperty.Register<SelectingItemsControl, object>("SelectedItem", coerce: CoerceSelectedItem);

        static SelectingItemsControl()
        {
            FocusableProperty.OverrideDefaultValue(typeof(SelectingItemsControl), true);

            SelectedIndexProperty.Changed.Subscribe(x =>
            {
                var control = x.Sender as SelectingItemsControl;

                if (control != null)
                {
                    var index = (int)x.NewValue;

                    if (index == -1)
                    {
                        control.SelectedItem = null;
                    }
                    else
                    {
                        control.SelectedItem = control.Items.ElementAt((int)x.NewValue);
                    }
                }
            });

            SelectedItemProperty.Changed.Subscribe(x =>
            {
                var control = x.Sender as SelectingItemsControl;

                if (control != null)
                {
                    control.SelectedItemChanged(x.NewValue);
                }
            });
        }

        public int SelectedIndex
        {
            get { return this.GetValue(SelectedIndexProperty); }
            set { this.SetValue(SelectedIndexProperty, value); }
        }

        public object SelectedItem
        {
            get { return this.GetValue(SelectedItemProperty); }
            set { this.SetValue(SelectedItemProperty, value); }
        }

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

        private static int CoerceSelectedIndex(PerspexObject o, int value)
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

        private static object CoerceSelectedItem(PerspexObject o, object value)
        {
            var control = o as SelectingItemsControl;

            if (control != null)
            {
                if (value != null && (control.Items == null || control.Items.IndexOf(value) == -1))
                {
                    return -1;
                }
            }

            return value;
        }

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
