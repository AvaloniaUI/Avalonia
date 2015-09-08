// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Perspex.Controls.Generators;
using Perspex.Input;
using Perspex.VisualTree;

namespace Perspex.Controls
{
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

        public new ITreeItemContainerGenerator ItemContainerGenerator => (ITreeItemContainerGenerator)base.ItemContainerGenerator;

        public object SelectedItem
        {
            get { return GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        protected override IItemContainerGenerator CreateItemContainerGenerator()
        {
            return new TreeItemContainerGenerator<TreeViewItem>(this);
        }

        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            var control = (IControl)e.Source;
            var item = ItemContainerGenerator.ItemFromContainer(control);

            if (item != null)
            {
                SelectedItem = item;
                e.Handled = true;
            }
        }

        private void SelectedItemChanged(object selected)
        {
            var containers = ItemContainerGenerator.GetAllContainers().OfType<ISelectable>();
            var selectedContainer = (selected != null) ?
                ItemContainerGenerator.ContainerFromItem(selected) :
                null;

            if (Presenter != null && Presenter.Panel != null)
            {
                KeyboardNavigation.SetTabOnceActiveElement(
                    (InputElement)Presenter.Panel,
                    selectedContainer);
            }

            foreach (var item in containers)
            {
                item.IsSelected = item == selectedContainer;
            }
        }
    }
}
