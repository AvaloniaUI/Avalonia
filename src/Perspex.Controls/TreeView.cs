// -----------------------------------------------------------------------
// <copyright file="TreeView.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Perspex.Controls.Generators;
    using Perspex.Input;
    using Perspex.VisualTree;

    public class TreeView : ItemsControl
    {
        public static readonly PerspexProperty<object> SelectedItemProperty =
            PerspexProperty.Register<TreeView, object>("SelectedItem");

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

        public new ITreeItemContainerGenerator ItemContainerGenerator
        {
            get { return (ITreeItemContainerGenerator)base.ItemContainerGenerator; }
        }

        public object SelectedItem
        {
            get { return this.GetValue(SelectedItemProperty); }
            set { this.SetValue(SelectedItemProperty, value); }
        }

        protected override IItemContainerGenerator CreateItemContainerGenerator()
        {
            return new TreeItemContainerGenerator<TreeViewItem>(this);
        }

        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            var control = (IControl)e.Source;
            var item = this.ItemContainerGenerator.ItemFromContainer(control);

            if (item != null)
            {
                this.SelectedItem = item;
                e.Handled = true;
            }
        }

        private void SelectedItemChanged(object selected)
        {
            var containers = this.ItemContainerGenerator.GetAllContainers().OfType<ISelectable>();
            var selectedContainer = (selected != null) ?
                this.ItemContainerGenerator.ContainerFromItem(selected) :
                null;

            if (this.Presenter != null && this.Presenter.Panel != null)
            {
                KeyboardNavigation.SetTabOnceActiveElement(
                    (InputElement)this.Presenter.Panel, 
                    selectedContainer);
            }

            foreach (var item in containers)
            {
                item.IsSelected = item == selectedContainer;
            }
        }
    }
}
