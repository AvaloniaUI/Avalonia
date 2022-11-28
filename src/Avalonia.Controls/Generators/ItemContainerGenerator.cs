using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Styling;

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
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        /// <inheritdoc/>
        public IEnumerable<ItemContainerInfo> Containers => _containers.Values;

        /// <inheritdoc/>
        public event EventHandler<ItemContainerEventArgs>? Materialized;

        /// <inheritdoc/>
        public event EventHandler<ItemContainerEventArgs>? Dematerialized;

        /// <inheritdoc/>
        public event EventHandler<ItemContainerEventArgs>? Recycled;

        /// <summary>
        /// Gets or sets the theme to be applied to the items in the control.
        /// </summary>
        public ControlTheme? ItemContainerTheme { get; set; }

        /// <summary>
        /// Gets or sets the data template used to display the items in the control.
        /// </summary>
        public IDataTemplate? ItemTemplate { get; set; }
        
        /// <inheritdoc />
        public IBinding? DisplayMemberBinding { get; set; }

        /// <summary>
        /// Gets the owner control.
        /// </summary>
        public IControl Owner { get; }

        /// <inheritdoc/>
        public virtual Type? ContainerType => null;

        /// <inheritdoc/>
        public ItemContainerInfo Materialize(int index, object item)
        {
            var container = new ItemContainerInfo(CreateContainer(item)!, item, index);

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
                    .ToArray();

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
                    if (_containers.TryGetValue(i, out var found))
                    {
                        result.Add(found);
                    }

                    _containers.Remove(i);
                }

                var toMove = _containers.Where(x => x.Key >= startingIndex)
                                        .OrderBy(x => x.Key).ToArray();

                foreach (var i in toMove)
                {
                    _containers.Remove(i.Key);
                    i.Value.Index -= count;
                    _containers.Add(i.Value.Index, i.Value);
                }

                Dematerialized?.Invoke(this, new ItemContainerEventArgs(startingIndex, result));

                if (toMove.Length > 0)
                {
                    var containers = toMove.Select(x => x.Value).ToArray();
                    Recycled?.Invoke(this, new ItemContainerEventArgs(containers[0].Index, containers));
                }
            }

            return result;
        }

        /// <inheritdoc/>
        public virtual bool TryRecycle(int oldIndex, int newIndex, object item) => false;

        /// <inheritdoc/>
        public virtual IEnumerable<ItemContainerInfo> Clear()
        {
            var result = Containers.ToArray();
            _containers.Clear();

            if (result.Length > 0)
            {
                Dematerialized?.Invoke(this, new ItemContainerEventArgs(0, result));
            }

            return result;
        }

        /// <inheritdoc/>
        public IControl? ContainerFromIndex(int index)
        {
            ItemContainerInfo? result;
            _containers.TryGetValue(index, out result);
            return result?.ContainerControl;
        }

        /// <inheritdoc/>
        public int IndexFromContainer(IControl? container)
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
        protected virtual IControl? CreateContainer(object item)
        {
            var result = item as IControl;

            if (result == null)
            {
                result = new ContentPresenter();
                if (DisplayMemberBinding is not null)
                {
                    result.SetValue(StyledElement.DataContextProperty, item, BindingPriority.Style);
                    result.Bind(ContentPresenter.ContentProperty, DisplayMemberBinding, BindingPriority.Style);
                }
                else
                {
                    result.SetValue(ContentPresenter.ContentProperty, item, BindingPriority.Style);
                }

                if (ItemTemplate != null)
                {
                    result.SetValue(
                        ContentPresenter.ContentTemplateProperty,
                        ItemTemplate,
                        BindingPriority.Style);
                }
            }

            if (ItemContainerTheme != null)
            {
                result.SetValue(
                    StyledElement.ThemeProperty,
                    ItemContainerTheme,
                    BindingPriority.Template);
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
