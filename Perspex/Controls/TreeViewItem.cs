// -----------------------------------------------------------------------
// <copyright file="TreeViewItem.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    public class TreeViewItem : HeaderedItemsControl
    {
        public static readonly PerspexProperty<bool> IsExpandedProperty =
            PerspexProperty.Register<TreeViewItem, bool>("IsExpanded");

        TreeView parent;

        public bool IsExpanded
        {
            get { return this.GetValue(IsExpandedProperty); }
            set { this.SetValue(IsExpandedProperty, value); }
        }

        protected override Control CreateItemControlOverride(object item)
        {
            if (this.parent != null)
            {
                return this.parent.CreateItemControl(item);
            }
            else
            {
                return null;
            }
        }

        protected override void OnAttachedToVisualTree()
        {
            base.OnAttachedToVisualTree();
            this.parent = this.GetVisualParent<Control>().TemplatedParent as TreeView;
        }
    }
}
