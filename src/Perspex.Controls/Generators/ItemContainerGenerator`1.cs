// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Controls.Templates;

namespace Perspex.Controls.Generators
{
    /// <summary>
    /// Creates containers for items and maintains a list of created containers.
    /// </summary>
    /// <typeparam name="T">The type of the container.</typeparam>
    public class ItemContainerGenerator<T> : ItemContainerGenerator where T : class, IContentControl, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ItemContainerGenerator{T}"/> class.
        /// </summary>
        /// <param name="owner">The owner control.</param>
        public ItemContainerGenerator(Control owner)
            : base(owner)
        {
        }

        /// <inheritdoc/>
        protected override IControl CreateContainer(object item, IDataTemplate itemTemplate)
        {
            T result = item as T;

            if (result == null)
            {
                result = new T();
                result.Content = Owner.MaterializeDataTemplate(item);
            }

            return result;
        }
    }
}
