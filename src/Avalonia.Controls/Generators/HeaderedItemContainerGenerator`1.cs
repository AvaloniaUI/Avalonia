// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq.Expressions;
using System.Reflection;
using Avalonia.Controls.Templates;
using Avalonia.Data;

namespace Avalonia.Controls.Generators
{
    /// <summary>
    /// Creates containers for headered items and maintains a list of created containers.
    /// </summary>
    /// <typeparam name="T">The type of the container.</typeparam>
    public class HeaderedItemContainerGenerator<T> : ItemContainerGenerator<T> where T : class, IControl, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ItemContainerGenerator{T}"/> class.
        /// </summary>
        /// <param name="owner">The owner control.</param>
        /// <param name="contentProperty">The container's Content property.</param>
        /// <param name="contentTemplateProperty">The container's ContentTemplate property.</param>
        /// <param name="headerProperty">The container's Header property.</param>
        /// <param name="headerTemplateProperty">The container's HeaderTemplate property.</param>
        public HeaderedItemContainerGenerator(
            IControl owner, 
            AvaloniaProperty contentProperty,
            AvaloniaProperty contentTemplateProperty,
            AvaloniaProperty headerProperty,
            AvaloniaProperty headerTemplateProperty)
            : base(owner, contentProperty, contentTemplateProperty)
        {
            Contract.Requires<ArgumentNullException>(headerProperty != null);

            HeaderProperty = headerProperty;
            HeaderTemplateProperty = headerTemplateProperty;
        }

        /// <summary>
        /// Gets the container's Content property.
        /// </summary>
        protected AvaloniaProperty HeaderProperty { get; }

        /// <summary>
        /// Gets the container's HeaderTemplate property.
        /// </summary>
        protected AvaloniaProperty HeaderTemplateProperty { get; }

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
                var result = base.CreateContainer(item);

                if (HeaderTemplateProperty != null)
                {
                    result.SetValue(HeaderTemplateProperty, ItemTemplate, BindingPriority.Style);
                }

                result.SetValue(HeaderProperty, item, BindingPriority.Style);

                return result;
            }
        }
    }
}
