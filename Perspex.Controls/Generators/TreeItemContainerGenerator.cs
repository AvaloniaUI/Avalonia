// -----------------------------------------------------------------------
// <copyright file="TreeItemContainerGenerator.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Generators
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public class TreeItemContainerGenerator<T> : ItemContainerGenerator, IItemContainerGenerator where T : TreeViewItem, new()
    {
        public TreeItemContainerGenerator(ItemsControl owner)
            : base(owner)
        {
        }

        IEnumerable<Control> IItemContainerGenerator.Remove(IEnumerable items)
        {
            var result = new List<Control>();

            foreach (var item in items)
            {
                var container = (T)this.GetContainerForItem(item);
                this.Remove(container, result);
            }

            return result;
        }

        protected override Control CreateContainerOverride(object item)
        {
            T result = item as T;

            if (result == null)
            {
                TreeDataTemplate template = this.GetTreeDataTemplate(item);

                result = new T
                {
                    Header = template.Build(item),
                    Items = template.ItemsSelector(item),
                    IsExpanded = template.IsExpanded(item),
                };
            }

            return result;
        }

        private TreeDataTemplate GetTreeDataTemplate(object item)
        {
            DataTemplate template = this.Owner.FindDataTemplate(item);

            if (template == null)
            {
                template = DataTemplate.Default;
            }

            TreeDataTemplate treeTemplate = template as TreeDataTemplate;

            if (treeTemplate == null)
            {
                treeTemplate = new TreeDataTemplate(template.Build, x => null);
            }

            return treeTemplate;
        }

        private void Remove(T container, List<Control> removed)
        {
            if (container.Items != null)
            {
                foreach (var childItem in container.Items)
                {
                    var childContainer = (T)this.GetContainerForItem(childItem);

                    if (childContainer != null)
                    {
                        this.Remove(childContainer, removed);
                    }
                }
            }

            this.RemoveByContainerInternal(container);
            removed.Add(container);
        }
    }
}
