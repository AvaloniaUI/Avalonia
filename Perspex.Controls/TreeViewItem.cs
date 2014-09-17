// -----------------------------------------------------------------------
// <copyright file="TreeViewItem.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System.Linq;

    public class TreeViewItem : HeaderedItemsControl
    {
        public static readonly PerspexProperty<bool> IsExpandedProperty =
            PerspexProperty.Register<TreeViewItem, bool>("IsExpanded");

        public static readonly PerspexProperty<bool> IsSelectedProperty =
            PerspexProperty.Register<TreeViewItem, bool>("IsSelected");

        TreeView parent;

        public TreeViewItem()
        {
            this.AddPseudoClass(IsSelectedProperty, ":selected");
            AffectsRender(IsSelectedProperty);
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

        protected override void OnVisualParentChanged(Visual oldParent)
        {
            this.parent = this.GetVisualAncestors().OfType<TreeView>().FirstOrDefault();
        }
    }
}
