// -----------------------------------------------------------------------
// <copyright file="TreeItemContainerGenerator.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Generators
{
    public class TreeItemContainerGenerator<T> : ItemContainerGenerator where T : TreeViewItem, new()
    {
        public TreeItemContainerGenerator(ItemsControl owner)
            : base(owner)
        {
        }

        protected override Control CreateContainerOverride(object item)
        {
            T result = item as T;

            if (result == null)
            {
                TreeDataTemplate template = this.GetTreeDataTemplate(item);

                System.Diagnostics.Debug.WriteLine("{0} created item for {1}", this.GetHashCode(), item);

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
    }
}
