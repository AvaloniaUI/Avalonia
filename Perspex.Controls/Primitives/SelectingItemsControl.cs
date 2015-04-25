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

    public class SelectingItemsControl : ItemsControl
    {
        public static readonly PerspexProperty<object> SelectedItemProperty =
            PerspexProperty.Register<SelectingItemsControl, object>("SelectedItem");

        static SelectingItemsControl()
        {
            FocusableProperty.OverrideDefaultValue(typeof(SelectingItemsControl), true);

            SelectedItemProperty.Changed.Subscribe(x =>
            {
                var control = x.Sender as SelectingItemsControl;

                if (control != null)
                {
                    control.SelectedItemChanged(x.NewValue);
                }
            });
        }

        public object SelectedItem
        {
            get { return this.GetValue(SelectedItemProperty); }
            set { this.SetValue(SelectedItemProperty, value); }
        }

        protected static int GetIndexOfItem(IEnumerable items, object item)
        {
            if (items != null)
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

        protected int GetIndexOfItem(object item)
        {
            return GetIndexOfItem(this.Items, item);
        }

        protected virtual void MoveSelection(FocusNavigationDirection direction)
        {
            if (this.SelectedItem != null)
            {
                int offset = 0;

                switch (direction)
                {
                    case FocusNavigationDirection.Up:
                    case FocusNavigationDirection.Left:
                        offset = -1;
                        break;
                    case FocusNavigationDirection.Down:
                    case FocusNavigationDirection.Right:
                        offset = 1;
                        break;
                }

                if (offset != 0)
                {
                    var currentIndex = GetIndexOfItem(this.SelectedItem);
                    var index = currentIndex + offset;

                    if (index >= 0 && index < this.Items.Cast<object>().Count())
                    {
                        this.SelectedItem = this.Items.Cast<object>().ElementAt(index);
                    }
                }
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

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
            }

            e.Handled = true;
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
                }
            }

            e.Handled = true;
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
        }
    }
}
