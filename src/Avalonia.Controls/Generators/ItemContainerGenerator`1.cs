// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Controls.Templates;
using Avalonia.Data;

namespace Avalonia.Controls.Generators
{
    /// <summary>
    /// Creates containers for items and maintains a list of created containers.
    /// </summary>
    /// <typeparam name="T">The type of the container.</typeparam>
    public class ItemContainerGenerator<T> : ItemContainerGenerator where T : class, IControl, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ItemContainerGenerator{T}"/> class.
        /// </summary>
        /// <param name="owner">The owner control.</param>
        /// <param name="contentProperty">The container's Content property.</param>
        /// <param name="contentTemplateProperty">The container's ContentTemplate property.</param>
        public ItemContainerGenerator(
            IControl owner, 
            AvaloniaProperty contentProperty,
            AvaloniaProperty contentTemplateProperty)
            : base(owner)
        {
            Contract.Requires<ArgumentNullException>(owner != null);
            Contract.Requires<ArgumentNullException>(contentProperty != null);

            ContentProperty = contentProperty;
            ContentTemplateProperty = contentTemplateProperty;
        }

        /// <inheritdoc/>
        public override Type ContainerType => typeof(T);

        /// <summary>
        /// Gets the container's Content property.
        /// </summary>
        protected AvaloniaProperty ContentProperty { get; }

        /// <summary>
        /// Gets the container's ContentTemplate property.
        /// </summary>
        protected AvaloniaProperty ContentTemplateProperty { get; }

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
                var result = new T();

                if (ContentTemplateProperty != null)
                {
                    result.SetValue(ContentTemplateProperty, ItemTemplate, BindingPriority.Style);
                }

                result.SetValue(ContentProperty, item, BindingPriority.Style);

                if (!(item is IControl))
                {
                    result.DataContext = item;
                }

                return result;
            }
        }

        /// <inheritdoc/>
        public override bool TryRecycle(int oldIndex, int newIndex, object item)
        {
            var container = ContainerFromIndex(oldIndex);

            if (container == null)
            {
                throw new IndexOutOfRangeException("Could not recycle container: not materialized.");
            }

            container.SetValue(ContentProperty, item);

            if (!(item is IControl))
            {
                container.DataContext = item;
            }

            var info = MoveContainer(oldIndex, newIndex, item);
            RaiseRecycled(new ItemContainerEventArgs(info));

            return true;
        }
    }
}
