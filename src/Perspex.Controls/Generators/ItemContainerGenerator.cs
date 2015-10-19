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
        private List<IControl> _containers = new List<IControl>();

        private readonly Subject<ItemContainers> _containersInitialized = new Subject<ItemContainers>();

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
        public IEnumerable<IControl> Containers => _containers;

        /// <inheritdoc/>
        public IObservable<ItemContainers> ContainersInitialized => _containersInitialized;

        /// <summary>
        /// Gets the owner control.
        /// </summary>
        public IControl Owner { get; }

        /// <inheritdoc/>
        public IEnumerable<IControl> Materialize(
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

        /// <inheritdoc/>
        public virtual IEnumerable<IControl> Dematerialize(int startingIndex, int count)
        {
            var result = new List<IControl>();

            for (int i = startingIndex; i < startingIndex + count; ++i)
            {
                if (i < _containers.Count)
                {
                    result.Add(_containers[i]);
                    _containers[i] = null;
                }
            }

            return result;
        }

        /// <inheritdoc/>
        public virtual IEnumerable<IControl> RemoveRange(int startingIndex, int count)
        {
            var result = _containers.GetRange(startingIndex, count);
            _containers.RemoveRange(startingIndex, count);
            return result;
        }

        /// <inheritdoc/>
        public virtual IEnumerable<IControl> Clear()
        {
            var result = _containers;
            _containers = new List<IControl>();
            return result;
        }

        /// <inheritdoc/>
        public IControl ContainerFromIndex(int index)
        {
            if (index < _containers.Count)
            {
                return _containers[index];
            }

            return null;
        }

        /// <inheritdoc/>
        public int IndexFromContainer(IControl container)
        {
            return _containers.IndexOf(container);
        }

        /// <summary>
        /// Creates the container for an item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>The created container control.</returns>
        protected virtual IControl CreateContainer(object item)
        {
            var result = Owner.MaterializeDataTemplate(item);

            if (result != null && !(item is IControl))
            {
                result.DataContext = item;
            }

            return result;
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
                while (_containers.Count < index)
                {
                    _containers.Add(null);
                }

                if (_containers.Count == index)
                {
                    _containers.Add(c);
                }
                else if (_containers[index] == null)
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

        /// <summary>
        /// Gets all containers with an index that fall within a range.
        /// </summary>
        /// <param name="index">The first index.</param>
        /// <param name="count">The number of elements in the range.</param>
        /// <returns>The containers.</returns>
        protected IEnumerable<IControl> GetContainerRange(int index, int count)
        {
            return _containers.GetRange(index, count);
        }
    }
}