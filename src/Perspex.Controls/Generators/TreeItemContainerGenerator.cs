// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using Perspex.Controls.Templates;

namespace Perspex.Controls.Generators
{
    /// <summary>
    /// Creates containers for tree items and maintains a list of created containers.
    /// </summary>
    /// <typeparam name="T">The type of the container.</typeparam>
    public class TreeItemContainerGenerator<T> : ITreeItemContainerGenerator where T : TreeViewItem, new()
    {
        private readonly Dictionary<object, T> _containers = new Dictionary<object, T>();

        private readonly Subject<ItemContainers> _containersInitialized = new Subject<ItemContainers>();

        /// <summary>
        /// Initializes a new instance of the <see cref="TreeItemContainerGenerator{T}"/> class.
        /// </summary>
        /// <param name="owner">The owner control.</param>
        public TreeItemContainerGenerator(IControl owner)
        {
            Owner = owner;
        }

        /// <summary>
        /// Signalled whenever new containers are initialized.
        /// </summary>
        public IObservable<ItemContainers> ContainersInitialized => _containersInitialized;

        /// <summary>
        /// Gets the owner control.
        /// </summary>
        public IControl Owner { get; }

        /// <summary>
        /// Creates container controls for a collection of items.
        /// </summary>
        /// <param name="startingIndex">
        /// The index of the first item of the data in the containing collection.
        /// </param>
        /// <param name="items">The items.</param>
        /// <returns>The created container controls.</returns>
        public IList<IControl> CreateContainers(int startingIndex, IEnumerable items)
        {
            Contract.Requires<ArgumentNullException>(items != null);

            int index = startingIndex;
            var result = new List<IControl>();

            foreach (var item in items)
            {
                var container = CreateContainer(item);
                _containers.Add(item, container);
                result.Add(container);
            }

            _containersInitialized.OnNext(new ItemContainers(startingIndex, result));

            return result.Where(x => x != null).ToList();
        }

        /// <summary>
        /// Removes a set of created containers from the index and returns the removed controls.
        /// </summary>
        /// <param name="startingIndex">
        /// The index of the first item of the data in the containing collection.
        /// </param>
        /// <param name="items">The items.</param>
        /// <returns>The removed controls.</returns>
        public IList<IControl> RemoveContainers(int startingIndex, IEnumerable items)
        {
            var result = new List<IControl>();

            foreach (var item in items)
            {
                T container;

                if (_containers.TryGetValue(item, out container))
                {
                    Remove(container, result);
                }
            }

            return result;
        }

        /// <summary>
        /// Clears the created containers from the index and returns the removed controls.
        /// </summary>
        /// <returns>The removed controls.</returns>
        public IList<IControl> ClearContainers()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the container control representing the item with the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The container or null if no container created.</returns>
        public IControl ContainerFromIndex(int index)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the index of the specified container control.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <returns>The index of the container or -1 if not found.</returns>
        public int IndexFromContainer(IControl container)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets all of the generated container controls.
        /// </summary>
        /// <returns>The containers.</returns>
        public IEnumerable<IControl> GetAllContainers()
        {
            return _containers.Values;
        }

        /// <summary>
        /// Gets the item that is contained by the specified container.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <returns>The item.</returns>
        public object ItemFromContainer(IControl container)
        {
            return container.DataContext;
        }

        /// <summary>
        /// Gets the container for the specified item
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>The container.</returns>
        public IControl ContainerFromItem(object item)
        {
            T result;
            _containers.TryGetValue(item, out result);
            return result;
        }

        /// <summary>
        /// Creates the container for an item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>The created container control.</returns>
        protected virtual T CreateContainer(object item)
        {
            T result = item as T;

            if (result == null)
            {
                var template = GetTreeDataTemplate(item);

                result = new T
                {
                    Header = template.Build(item),
                    Items = template.ItemsSelector(item),
                    IsExpanded = template.IsExpanded(item),
                    DataContext = item,
                };
            }

            return result;
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
                template = DataTemplate.Default;
            }

            var treeTemplate = template as ITreeDataTemplate;

            if (treeTemplate == null)
            {
                treeTemplate = new TreeDataTemplate(typeof(object), template.Build, x => null);
            }

            return treeTemplate;
        }

        private void Remove(T container, IList<IControl> removed)
        {
            if (container.Items != null)
            {
                foreach (var childItem in container.Items)
                {
                    T childContainer;

                    if (_containers.TryGetValue(childItem, out childContainer))
                    {
                        Remove(childContainer, removed);
                    }
                }
            }

            // TODO: Dual index.
            var i = _containers.FirstOrDefault(x => x.Value == container);

            if (i.Key != null)
            {
                _containers.Remove(i.Key);
            }
        }
    }
}
