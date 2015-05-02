// -----------------------------------------------------------------------
// <copyright file="TreeView.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using Perspex.Controls.Generators;
    using Perspex.Input;
    using Perspex.VisualTree;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public class TreeView : ItemsControl
    {
        public static readonly PerspexProperty<object> SelectedItemProperty =
            PerspexProperty.Register<TreeView, object>("SelectedItem", coerce: CoerceSelectedItem);

        static TreeView()
        {
            SelectedItemProperty.Changed.Subscribe(x =>
            {
                var control = x.Sender as TreeView;

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

        protected override ItemContainerGenerator CreateItemContainerGenerator()
        {
            return new TreeItemContainerGenerator<TreeViewItem>(this);
        }

        protected virtual void MoveSelection(FocusNavigationDirection direction)
        {
            // TODO: Up and down movement is a *HACK* and probably pretty slow. Probably needs
            // rewriting at some point.
            if (this.SelectedItem != null)
            {
                switch (direction)
                {
                    case FocusNavigationDirection.Up:
                        {
                            var list = this.Flatten();
                            var index = list.IndexOf(this.SelectedItem);

                            if (index > 0)
                            {
                                this.SelectedItem = list[index - 1];
                            }

                            break;
                        }

                    case FocusNavigationDirection.Down:
                        {
                            var list = this.Flatten();
                            var index = list.IndexOf(this.SelectedItem);

                            if (index + 1 < list.Count)
                            {
                                this.SelectedItem = list[index + 1];
                            }

                            break;
                        }

                    case FocusNavigationDirection.Left:
                        {
                            var node = (TreeViewItem)this.ItemContainerGenerator.GetContainerForItem(this.SelectedItem);
                            node.IsExpanded = false;
                            break;
                        }

                    case FocusNavigationDirection.Right:
                        {
                            var node = (TreeViewItem)this.ItemContainerGenerator.GetContainerForItem(this.SelectedItem);
                            node.IsExpanded = true;
                            break;
                        }
                }
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

        private static object CoerceSelectedItem(PerspexObject o, object value)
        {
            //var control = o as SelectingItemsControl;

            //if (control != null)
            //{
            //    if (value != null && (control.Items == null || control.Items.IndexOf(value) == -1))
            //    {
            //        return null;
            //    }
            //}

            return value;
        }

        private List<object> Flatten()
        {
            var result = new List<object>();
            this.Flatten(this.Items, result);
            return result;
        }

        private void Flatten(IEnumerable items, List<object> result)
        {
            if (items != null)
            {
                foreach (object item in items)
                {
                    var container = (TreeViewItem)this.ItemContainerGenerator.GetContainerForItem(item);
                    result.Add(item);

                    if (container.IsExpanded)
                    {
                        this.Flatten(container.Items, result);
                    }
                }
            }
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
