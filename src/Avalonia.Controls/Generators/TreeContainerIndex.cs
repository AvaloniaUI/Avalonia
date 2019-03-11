// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Controls.Generators
{
    /// <summary>
    /// Maintains an index of all item containers currently materialized by a <see cref="TreeView"/>.
    /// </summary>
    /// <remarks>
    /// Each <see cref="TreeViewItem"/> has its own <see cref="TreeItemContainerGenerator{T}"/> 
    /// that maintains the list of its direct children, but they also share an instance of this
    /// class in their <see cref="TreeItemContainerGenerator{T}.Index"/> property which tracks 
    /// the containers materialized for the entire tree.
    /// </remarks>
    public class TreeContainerIndex
    {
        private readonly Dictionary<object, IControl> _itemToContainer = new Dictionary<object, IControl>();
        private readonly Dictionary<IControl, object> _containerToItem = new Dictionary<IControl, object>();

        /// <summary>
        /// Signaled whenever new containers are materialized.
        /// </summary>
        public event EventHandler<ItemContainerEventArgs> Materialized;

        /// <summary>
        /// Event raised whenever containers are dematerialized.
        /// </summary>
        public event EventHandler<ItemContainerEventArgs> Dematerialized;

        /// <summary>
        /// Gets the currently materialized containers.
        /// </summary>
        public IEnumerable<IControl> Containers => _containerToItem.Keys;

        /// <summary>
        /// Gets the items of currently materialized containers.
        /// </summary>
        public IEnumerable<object> Items => _containerToItem.Values;

        /// <summary>
        /// Adds an entry to the index.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="container">The item container.</param>
        public void Add(object item, IControl container)
        {
            _itemToContainer.Add(item, container);
            _containerToItem.Add(container, item);

            Materialized?.Invoke(
                this, 
                new ItemContainerEventArgs(new ItemContainerInfo(container, item, 0)));
        }

        /// <summary>
        /// Removes a container from the index.
        /// </summary>
        /// <param name="container">The item container.</param>
        public void Remove(IControl container)
        {
            var item = _containerToItem[container];
            _containerToItem.Remove(container);
            _itemToContainer.Remove(item);

            Dematerialized?.Invoke(
                this, 
                new ItemContainerEventArgs(new ItemContainerInfo(container, item, 0)));
        }

        /// <summary>
        /// Removes a set of containers from the index.
        /// </summary>
        /// <param name="startingIndex">The index of the first item.</param>
        /// <param name="containers">The item containers.</param>
        public void Remove(int startingIndex, IEnumerable<ItemContainerInfo> containers)
        {
            foreach (var container in containers)
            {
                var item = _containerToItem[container.ContainerControl];
                _containerToItem.Remove(container.ContainerControl);
                _itemToContainer.Remove(item);
            }

            Dematerialized?.Invoke(
                this,
                new ItemContainerEventArgs(startingIndex, containers.ToList()));
        }

        /// <summary>
        /// Gets the container for an item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>The container, or null of not found.</returns>
        public IControl ContainerFromItem(object item)
        {
            IControl result;
            _itemToContainer.TryGetValue(item, out result);
            return result;
        }

        /// <summary>
        /// Gets the item for a container.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <returns>The item, or null of not found.</returns>
        public object ItemFromContainer(IControl container)
        {
            object result;
            _containerToItem.TryGetValue(container, out result);
            return result;
        }
    }
}
