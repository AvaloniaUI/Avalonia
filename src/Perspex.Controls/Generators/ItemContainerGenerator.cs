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
    /// Creates containers for items and maintains a list of created containers.
    /// </summary>
    public class ItemContainerGenerator : IItemContainerGenerator
    {
        private Dictionary<int, IControl> _containers = new Dictionary<int, IControl>();

        private readonly Subject<ItemContainers> _containersInitialized = new Subject<ItemContainers>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemContainerGenerator"/> class.
        /// </summary>
        /// <param name="owner">The owner control.</param>
        public ItemContainerGenerator(IControl owner)
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
        /// <param name="selector">An optional member selector.</param>
        /// <returns>The created container controls.</returns>
        public IList<IControl> CreateContainers(
            int startingIndex,
            IEnumerable items,
            IMemberSelector selector)
        {
            Contract.Requires<ArgumentNullException>(items != null);

            int index = startingIndex;
            var result = new List<IControl>();

            foreach (var item in items)
            {
                var i = selector != null ? selector.Select(item) : item;
                var container = CreateContainer(i);
                result.Add(container);
            }

            AddContainers(startingIndex, result);
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
            var count = items.Cast<object>().Count();

            for (int i = startingIndex; i < startingIndex + count; ++i)
            {
                var container = _containers[i];

                if (container != null)
                {
                    result.Add(container);
                    _containers.Remove(i);
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
            var result = _containers;
            _containers = new Dictionary<int, IControl>();
            return result.Values.ToList();
        }

        /// <summary>
        /// Gets the container control representing the item with the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The container or null if no container created.</returns>
        public IControl ContainerFromIndex(int index)
        {
            IControl result;
            _containers.TryGetValue(index, out result);
            return result;
        }

        /// <summary>
        /// Gets the index of the specified container control.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <returns>The index of the container or -1 if not found.</returns>
        public int IndexFromContainer(IControl container)
        {
            foreach (var i in _containers)
            {
                if (i.Value == container)
                {
                    return i.Key;
                }
            }

            return -1;
        }

        /// <summary>
        /// Creates the container for an item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>The created container control.</returns>
        protected virtual IControl CreateContainer(object item)
        {
            return Owner.MaterializeDataTemplate(item);
        }

        /// <summary>
        /// Adds a collection of containers to the index.
        /// </summary>
        /// <param name="index">The starting index.</param>
        /// <param name="container">The container.</param>
        protected void AddContainers(int index, IList<IControl> container)
        {
            Contract.Requires<ArgumentNullException>(container != null);

            foreach (var c in container)
            {
                if (!_containers.ContainsKey(index))
                {
                    _containers[index] = c;
                }
                else
                {
                    throw new InvalidOperationException("Container already created.");
                }

                ++index;
            }
        }
    }
}