// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
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
        private readonly Dictionary<object, T> _itemToContainer;
        private readonly Dictionary<IControl, object> _containerToItem;

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

            if (rootGenerator == null)
            {
                _itemToContainer = new Dictionary<object, T>();
                _containerToItem = new Dictionary<IControl, object>();
            }
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

        /// <summary>
        /// Gets the item container for the specified item, anywhere in the tree.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>The container, or null if not found.</returns>
        public IControl TreeContainerFromItem(object item)
        {
            T result;
            _itemToContainer.TryGetValue(item, out result);
            return result;
        }

        /// <summary>
        /// Gets the item for the specified item container, anywhere in the tree.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <returns>The item, or null if not found.</returns>
        public object TreeItemFromContainer(IControl container)
        {
            object result;
            _containerToItem.TryGetValue(container, out result);
            return result;
        }

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

                AddToIndex(item, result);

                return result;
            }
        }

        public override IEnumerable<IControl> Clear()
        {
            ClearIndex();
            return base.Clear();
        }

        public override IEnumerable<IControl> Dematerialize(int startingIndex, int count)
        {
            RemoveFromIndex(GetContainerRange(startingIndex, count));
            return base.Dematerialize(startingIndex, count);
        }

        private void AddToIndex(object item, T container)
        {
            if (RootGenerator != null)
            {
                ((TreeItemContainerGenerator<T>)RootGenerator).AddToIndex(item, container);
            }
            else
            {
                _itemToContainer.Add(item, container);
                _containerToItem.Add(container, item);
            }
        }

        private void RemoveFromIndex(IEnumerable<IControl> containers)
        {
            if (RootGenerator != null)
            {
                ((TreeItemContainerGenerator<T>)RootGenerator).RemoveFromIndex(containers);
            }
            else
            {
                foreach (var container in containers)
                {
                    var item = _containerToItem[container];
                    _containerToItem.Remove(container);
                    _itemToContainer.Remove(item);
                }
            }
        }

        private void ClearIndex()
        {
            if (RootGenerator != null)
            {
                ((TreeItemContainerGenerator<T>)RootGenerator).ClearIndex();
            }
            else
            {
                _containerToItem.Clear();
                _itemToContainer.Clear();
            }
        }

        /// <summary>
        /// Gets the data template for the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>The template.</returns>
        private ITreeDataTemplate GetTreeDataTemplate(object item)
        {
            var template = Owner.FindDataTemplate(item) ?? FuncDataTemplate.Default;
            var treeTemplate = template as ITreeDataTemplate ??
                new FuncTreeDataTemplate(typeof(object), template.Build, x => null);
            return treeTemplate;
        }
    }
}
