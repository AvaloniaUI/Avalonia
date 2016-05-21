// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using Avalonia.Controls.Templates;

namespace Avalonia.Controls.Generators
{
    /// <summary>
    /// Creates containers for items and maintains a list of created containers.
    /// </summary>
    public class ItemContainerGenerator : IItemContainerGenerator
    {
        private List<ItemContainerInfo> _containers = new List<ItemContainerInfo>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemContainerGenerator"/> class.
        /// </summary>
        /// <param name="owner">The owner control.</param>
        public ItemContainerGenerator(IControl owner)
        {
            Contract.Requires<ArgumentNullException>(owner != null);

            Owner = owner;
        }

        /// <inheritdoc/>
        public IEnumerable<ItemContainerInfo> Containers => _containers.Where(x => x != null);

        /// <inheritdoc/>
        public event EventHandler<ItemContainerEventArgs> Materialized;

        /// <inheritdoc/>
        public event EventHandler<ItemContainerEventArgs> Dematerialized;

        /// <summary>
        /// Gets or sets the data template used to display the items in the control.
        /// </summary>
        public IDataTemplate ItemTemplate { get; set; }

        /// <summary>
        /// Gets the owner control.
        /// </summary>
        public IControl Owner { get; }

        /// <inheritdoc/>
        public ItemContainerInfo Materialize(
            int index,
            object item,
            IMemberSelector selector)
        {
            var i = selector != null ? selector.Select(item) : item;
            var container = new ItemContainerInfo(CreateContainer(i), item, index);

            AddContainer(container);
            Materialized?.Invoke(this, new ItemContainerEventArgs(index, container));

            return container;
        }

        /// <inheritdoc/>
        public virtual IEnumerable<ItemContainerInfo> Dematerialize(int startingIndex, int count)
        {
            var result = new List<ItemContainerInfo>();

            for (int i = startingIndex; i < startingIndex + count; ++i)
            {
                if (i < _containers.Count)
                {
                    result.Add(_containers[i]);
                    _containers[i] = null;
                }
            }

            Dematerialized?.Invoke(this, new ItemContainerEventArgs(startingIndex, result));

            return result;
        }

        /// <inheritdoc/>
        public virtual void InsertSpace(int index, int count)
        {
            _containers.InsertRange(index, Enumerable.Repeat<ItemContainerInfo>(null, count));
        }

        /// <inheritdoc/>
        public virtual IEnumerable<ItemContainerInfo> RemoveRange(int startingIndex, int count)
        {
            List<ItemContainerInfo> result = new List<ItemContainerInfo>();

            if (startingIndex < _containers.Count)
            {
                result.AddRange(_containers.GetRange(startingIndex, count));
                _containers.RemoveRange(startingIndex, count);
                Dematerialized?.Invoke(this, new ItemContainerEventArgs(startingIndex, result));
            }

            return result;
        }

        /// <inheritdoc/>
        public virtual IEnumerable<ItemContainerInfo> Clear()
        {
            var result = _containers.Where(x => x != null).ToList();
            _containers = new List<ItemContainerInfo>();

            if (result.Count > 0)
            {
                Dematerialized?.Invoke(this, new ItemContainerEventArgs(0, result));
            }

            return result;
        }

        /// <inheritdoc/>
        public IControl ContainerFromIndex(int index)
        {
            if (index < _containers.Count)
            {
                return _containers[index]?.ContainerControl;
            }

            return null;
        }

        /// <inheritdoc/>
        public int IndexFromContainer(IControl container)
        {
            var index = 0;

            foreach (var i in _containers)
            {
                if (i?.ContainerControl == container)
                {
                    return index;
                }

                ++index;
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
            var result = Owner.MaterializeDataTemplate(item, ItemTemplate);

            if (result != null && !(item is IControl))
            {
                result.DataContext = item;
            }

            return result;
        }

        /// <summary>
        /// Adds a container to the index.
        /// </summary>
        /// <param name="container">The container.</param>
        protected void AddContainer(ItemContainerInfo container)
        {
            Contract.Requires<ArgumentNullException>(container != null);

            while (_containers.Count < container.Index)
            {
                _containers.Add(null);
            }

            if (_containers.Count == container.Index)
            {
                _containers.Add(container);
            }
            else if (_containers[container.Index] == null)
            {
                _containers[container.Index] = container;
            }
            else
            {
                throw new InvalidOperationException("Container already created.");
            }
        }

        /// <summary>
        /// Gets all containers with an index that fall within a range.
        /// </summary>
        /// <param name="index">The first index.</param>
        /// <param name="count">The number of elements in the range.</param>
        /// <returns>The containers.</returns>
        protected IEnumerable<ItemContainerInfo> GetContainerRange(int index, int count)
        {
            return _containers.GetRange(index, count);
        }
    }
}