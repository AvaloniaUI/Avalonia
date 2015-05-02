// -----------------------------------------------------------------------
// <copyright file="SelectingItemsControl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Primitives
{
    using System;
    using System.Collections;
    using System.Linq;
    using Perspex.Input;
    using Perspex.VisualTree;
    using System.Collections.Generic;

    public abstract class SelectingItemsControl : ItemsControl
    {
        public static readonly PerspexProperty<int> SelectedIndexProperty =
            PerspexProperty.Register<SelectingItemsControl, int>("SelectedIndex", coerce: CoerceSelectedIndex);

        public static readonly PerspexProperty<object> SelectedItemProperty =
            PerspexProperty.Register<SelectingItemsControl, object>("SelectedItem");

        static SelectingItemsControl()
        {
            FocusableProperty.OverrideDefaultValue(typeof(SelectingItemsControl), true);

            SelectedIndexProperty.Changed.Subscribe(x =>
            {
                var control = x.Sender as SelectingItemsControl;

                if (control != null)
                {
                    control.SelectedItem = control.GetItemAt((int)x.NewValue);
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

        protected static int GetIndexOfItem(IEnumerable items, object item)
        {
            var list = items as IList;

            if (list != null)
            {
                return list.IndexOf(item);
            }
            else if (items != null)
            {
                int index = 0;

                foreach (var i in items)
                {
                    if (object.ReferenceEquals(i, item))
                    {
                        return index;
                    }

                    ++index;
                }
            }

            return -1;
        }

        protected static object GetItemAt(IEnumerable items, int index)
        {
            var list = items as IList;

            if (list != null)
            {
                return list[index];
            }
            else if (items != null)
            {
                return items.Cast<object>().ElementAt(index);
            }

            return -1;
        }

        protected int GetIndexOfItem(object item)
        {
            return GetIndexOfItem(this.Items, item);
        }

        protected object GetItemAt(int index)
        {
            return GetItemAt(this.Items, index);
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
                    var count = control.Items.Cast<object>().Count();
                    return Math.Min(value, count - 1);
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
                this.SelectedIndex = GetIndexOfItem(selected);
            }
        }
    }
}
