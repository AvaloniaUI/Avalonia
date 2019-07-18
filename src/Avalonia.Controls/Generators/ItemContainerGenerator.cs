// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;

namespace Avalonia.Controls.Generators
{
    /// <summary>
    /// Creates containers for items and maintains a list of created containers.
    /// </summary>
    public class ItemContainerGenerator : IItemContainerGenerator
    {
        private SortedDictionary<int, ItemContainerInfo> _containers = new SortedDictionary<int, ItemContainerInfo>();

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
        public IEnumerable<ItemContainerInfo> Containers => _containers.Values;

        /// <inheritdoc/>
        public event EventHandler<ItemContainerEventArgs> Materialized;

        /// <inheritdoc/>
        public event EventHandler<ItemContainerEventArgs> Dematerialized;

        /// <inheritdoc/>
        public event EventHandler<ItemContainerEventArgs> Recycled;

        /// <summary>
        /// Gets or sets the data template used to display the items in the control.
        /// </summary>
        public IDataTemplate ItemTemplate { get; set; }

        /// <summary>
        /// Gets the owner control.
        /// </summary>
        public IControl Owner { get; }

        /// <inheritdoc/>
        public virtual Type ContainerType => null;

        /// <inheritdoc/>
        public ItemContainerInfo Materialize(int index, object item)
        {
            var container = new ItemContainerInfo(CreateContainer(item), item, index);

            _containers.Add(container.Index, container);
            Materialized?.Invoke(this, new ItemContainerEventArgs(container));

            return container;
        }

        /// <inheritdoc/>
        public virtual IEnumerable<ItemContainerInfo> Dematerialize(int startingIndex, int count)
        {
            var result = new List<ItemContainerInfo>();

            for (int i = startingIndex; i < startingIndex + count; ++i)
            {
                result.Add(_containers[i]);
                _containers.Remove(i);
            }

            Dematerialized?.Invoke(this, new ItemContainerEventArgs(startingIndex, result));

            return result;
        }

        /// <inheritdoc/>
        public virtual void InsertSpace(int index, int count)
        {
            if (count > 0)
            {
                var toMove = _containers.Where(x => x.Key >= index)
                    .OrderByDescending(x => x.Key)
                    .ToList();

                foreach (var i in toMove)
                {
                    _containers.Remove(i.Key);
                    i.Value.Index += count;
                    _containers.Add(i.Value.Index, i.Value);
                }
            }
        }

        /// <inheritdoc/>
        public virtual IEnumerable<ItemContainerInfo> RemoveRange(int startingIndex, int count)
        {
            var result = new List<ItemContainerInfo>();

            if (count > 0)
            {
                for (var i = startingIndex; i < startingIndex + count; ++i)
                {
                    ItemContainerInfo found;

                    if (_containers.TryGetValue(i, out found))
                    {
                        result.Add(found);
                    }

                    _containers.Remove(i);
                }

                var toMove = _containers.Where(x => x.Key >= startingIndex)
                                        .OrderBy(x => x.Key).ToList();

                foreach (var i in toMove)
                {
                    _containers.Remove(i.Key);
                    i.Value.Index -= count;
                    _containers.Add(i.Value.Index, i.Value);
                }

                Dematerialized?.Invoke(this, new ItemContainerEventArgs(startingIndex, result));
            }

            return result;
        }

        /// <inheritdoc/>
        public virtual bool TryRecycle(int oldIndex, int newIndex, object item) => false;

        /// <inheritdoc/>
        public virtual IEnumerable<ItemContainerInfo> Clear()
        {
            var result = Containers.ToList();
            _containers.Clear();

            if (result.Count > 0)
            {
                Dematerialized?.Invoke(this, new ItemContainerEventArgs(0, result));
            }

            return result;
        }

        /// <inheritdoc/>
        public IControl ContainerFromIndex(int index)
        {
            ItemContainerInfo result;
            _containers.TryGetValue(index, out result);
            return result?.ContainerControl;
        }

        /// <inheritdoc/>
        public int IndexFromContainer(IControl container)
        {
            foreach (var i in _containers)
            {
                if (i.Value.ContainerControl == container)
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
            var result = item as IControl;

            if (result == null)
            {
                result = new ContentPresenter();
                result.SetValue(ContentPresenter.ContentProperty, item, BindingPriority.Style);

                if (ItemTemplate != null)
                {
                    result.SetValue(
                        ContentPresenter.ContentTemplateProperty,
                        ItemTemplate,
                        BindingPriority.TemplatedParent);
                }
            }

            return result;
        }

        /// <summary>
        /// Moves a container.
        /// </summary>
        /// <param name="oldIndex">The old index.</param>
        /// <param name="newIndex">The new index.</param>
        /// <param name="item">The new item.</param>
        /// <returns>The container info.</returns>
        protected ItemContainerInfo MoveContainer(int oldIndex, int newIndex, object item)
        {
            var container = _containers[oldIndex];
            container.Index = newIndex;
            container.Item = item;
            _containers.Remove(oldIndex);
            _containers.Add(newIndex, container);
            return container;
        }

        /// <summary>
        /// Gets all containers with an index that fall within a range.
        /// </summary>
        /// <param name="index">The first index.</param>
        /// <param name="count">The number of elements in the range.</param>
        /// <returns>The containers.</returns>
        protected IEnumerable<ItemContainerInfo> GetContainerRange(int index, int count)
        {
            return _containers.Where(x => x.Key >= index && x.Key < index + count).Select(x => x.Value);
        }

        /// <summary>
        /// Raises the <see cref="Recycled"/> event.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected void RaiseRecycled(ItemContainerEventArgs e)
        {
            Recycled?.Invoke(this, e);
        }
    }
}
