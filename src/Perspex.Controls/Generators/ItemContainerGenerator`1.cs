// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq.Expressions;
using System.Reflection;
using Perspex.Controls.Templates;

namespace Perspex.Controls.Generators
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
        public ItemContainerGenerator(
            IControl owner, 
            PerspexProperty contentProperty)
            : base(owner)
        {
            Contract.Requires<ArgumentNullException>(owner != null);
            Contract.Requires<ArgumentNullException>(contentProperty != null);

            ContentProperty = contentProperty;
        }

        /// <summary>
        /// Gets the container's Content property.
        /// </summary>
        protected PerspexProperty ContentProperty { get; }

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
                result.SetValue(ContentProperty, item);

                if (!(item is IControl))
                {
                    result.DataContext = item;
                }

                return result;
            }
        }
    }
}
