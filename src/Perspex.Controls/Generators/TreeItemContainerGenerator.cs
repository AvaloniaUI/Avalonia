// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Controls.Templates;

namespace Perspex.Controls.Generators
{
    /// <summary>
    /// Creates containers for tree items and maintains a list of created containers.
    /// </summary>
    /// <typeparam name="T">The type of the container.</typeparam>
    public class TreeItemContainerGenerator<T> : ItemContainerGenerator<T>, ITreeItemContainerGenerator
        where T : class, IControl, new()
    {
        private ITreeItemContainerGenerator rootGenerator;

        /// <summary>
        /// Initializes a new instance of the <see cref="TreeItemContainerGenerator{T}"/> class.
        /// </summary>
        /// <param name="owner">The owner control.</param>
        /// <param name="contentProperty">The container's Content property.</param>
        /// <param name="itemsProperty">The container's Items property.</param>
        /// <param name="isExpandedProperty">The container's IsExpanded property.</param>
        /// <param name="rootGenerator">
        /// The item container for the root of the tree, or null if this generator is itself the
        /// root of the tree.
        /// </param>
        public TreeItemContainerGenerator(
            IControl owner,
            PerspexProperty contentProperty,
            PerspexProperty itemsProperty,
            PerspexProperty isExpandedProperty,
            ITreeItemContainerGenerator rootGenerator)
            : base(owner, contentProperty)
        {
            ItemsProperty = itemsProperty;
            IsExpandedProperty = isExpandedProperty;
            RootGenerator = rootGenerator;
        }

        /// <summary>
        /// Gets the item container for the root of the tree, or null if this generator is itself 
        /// the root of the tree.
        /// </summary>
        public ITreeItemContainerGenerator RootGenerator { get; }

        /// <summary>
        /// Gets the item container's Items property.
        /// </summary>
        protected PerspexProperty ItemsProperty { get; }

        /// <summary>
        /// Gets the item container's IsExpanded property.
        /// </summary>
        protected PerspexProperty IsExpandedProperty { get; }

        /// <inheritdoc/>
        protected override IControl CreateContainer(object item)
        {
            var container = item as T;

            if (item == null)
            {
                return null;
            }
            else if (container != null)
            {
                return container;
            }
            else
            {
                var template = GetTreeDataTemplate(item);
                var result = new T();

                result.SetValue(ContentProperty, template.Build(item));
                result.SetValue(ItemsProperty, template.ItemsSelector(item));
                result.SetValue(IsExpandedProperty, template.IsExpanded(item));

                if (!(item is IControl))
                {
                    result.DataContext = item;
                }

                return result;
            }
        }

        /// <summary>
        /// Gets the data template for the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>The template.</returns>
        private ITreeDataTemplate GetTreeDataTemplate(object item)
        {
            IDataTemplate template = Owner.FindDataTemplate(item);

            if (template == null)
            {
                template = FuncDataTemplate.Default;
            }

            var treeTemplate = template as ITreeDataTemplate;

            if (treeTemplate == null)
            {
                treeTemplate = new FuncTreeDataTemplate(typeof(object), template.Build, x => null);
            }

            return treeTemplate;
        }
    }
}
