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

    public class TreeView : SelectingItemsControl
    {
        protected override Control CreateItemControlOverride(object item)
        {
            TreeViewItem result = item as TreeViewItem;

            if (result == null)
            {
                TreeDataTemplate template = this.GetTreeDataTemplate(item);

                result = new TreeViewItem
                {
                    Header = template.Build(item),
                    Items = template.ItemsSelector(item),
                };
            }

            return result;
        }

        private TreeDataTemplate GetTreeDataTemplate(object item)
        {
            DataTemplate template = this.GetDataTemplate(item);
            TreeDataTemplate treeTemplate = template as TreeDataTemplate;

            if (treeTemplate == null)
            {
                treeTemplate = new TreeDataTemplate(template.Build, x => null);
            }

            return treeTemplate;
        }
    }
}
