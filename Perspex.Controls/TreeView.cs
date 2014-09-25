// -----------------------------------------------------------------------
// <copyright file="TreeView.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Collections;
    using System.Linq;
    using System.Reactive.Linq;
    using Perspex.Controls.Generators;
    using Perspex.Input;

    public class TreeView : SelectingItemsControl
    {
        public TreeView()
        {
            this.PointerPressed += this.OnPointerPressed;
        }

        protected override ItemContainerGenerator CreateItemContainerGenerator()
        {
            return new TreeItemContainerGenerator<TreeViewItem>(this);
        }

        private void OnPointerPressed(object sender, PointerEventArgs e)
        {
            IVisual source = (IVisual)e.Source;
            ContentPresenter contentPresenter = source.GetVisualAncestors()
                .OfType<ContentPresenter>()
                .FirstOrDefault();

            if (contentPresenter != null)
            {
                TreeViewItem item = contentPresenter.TemplatedParent as TreeViewItem;

                if (item != null)
                {
                    foreach (var i in this.GetVisualDescendents().OfType<TreeViewItem>())
                    {
                        i.IsSelected = i == item;
                    }

                    this.SelectedItem = this.ItemContainerGenerator.GetContainerForItem(item);
                }
            }

        }
    }
}
