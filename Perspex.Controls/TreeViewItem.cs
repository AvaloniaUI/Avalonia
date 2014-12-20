// -----------------------------------------------------------------------
// <copyright file="TreeViewItem.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Linq;
    using Perspex.Controls.Generators;
    using Perspex.Controls.Primitives;
    using Perspex.VisualTree;

    public class TreeViewItem : HeaderedItemsControl, ISelectable
    {
        public static readonly PerspexProperty<bool> IsExpandedProperty =
            PerspexProperty.Register<TreeViewItem, bool>("IsExpanded");

        public static readonly PerspexProperty<bool> IsSelectedProperty =
            ListBoxItem.IsSelectedProperty.AddOwner<TreeViewItem>();

        private TreeView treeView;

        static TreeViewItem()
        {
            Control.PseudoClass(IsSelectedProperty, ":selected");
        }

        public TreeViewItem()
        {
            Control.AffectsRender(IsSelectedProperty);
        }

        public bool IsExpanded
        {
            get { return this.GetValue(IsExpandedProperty); }
            set { this.SetValue(IsExpandedProperty, value); }
        }

        public bool IsSelected
        {
            get { return this.GetValue(IsSelectedProperty); }
            set { this.SetValue(IsSelectedProperty, value); }
        }

        protected override ItemContainerGenerator CreateItemContainerGenerator()
        {
            if (this.treeView == null)
            {
                throw new InvalidOperationException(
                    "Cannot get the ItemContainerGenerator for a TreeViewItem " + 
                    "before it is added to a TreeView.");
            }

            return this.treeView.ItemContainerGenerator;
        }

        protected override void OnVisualParentChanged(Visual oldParent)
        {
            if (this.GetVisualParent() != null)
            {
                this.treeView = this.GetVisualAncestors().OfType<TreeView>().FirstOrDefault();

                if (this.treeView == null)
                {
                    throw new InvalidOperationException("TreeViewItems must be added to a TreeView.");
                }
            }
            else
            {
                this.treeView = null;
            }
        }
    }
}
