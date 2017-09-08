// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Styling;

namespace Avalonia.Markup.Xaml.Data
{
    public class StyleResourceBinding : IBinding
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StyleResourceBinding"/> class.
        /// </summary>
        /// <param name="name">The resource name.</param>
        public StyleResourceBinding(string name)
        {
            Name = name;
        }

        /// <inheritdoc/>
        public BindingMode Mode => BindingMode.OneTime;

        /// <summary>
        /// Gets the resource name.
        /// </summary>
        public string Name { get; }

        /// <inheritdoc/>
        public BindingPriority Priority => BindingPriority.LocalValue;

        /// <inheritdoc/>
        public InstancedBinding Initiate(
            IAvaloniaObject target,
            AvaloniaProperty targetProperty,
            object anchor = null,
            bool enableDataValidation = false)
        {
            var host = (target as IControl) ?? (anchor as IControl);
            var style = anchor as IStyle;
            var resource = AvaloniaProperty.UnsetValue;

            if (host != null)
            {
                resource = host.FindResource(Name);
            }
            else if (style != null)
            {
                if (!style.TryGetResource(Name, out resource))
                {
                    resource = AvaloniaProperty.UnsetValue;
                }
            }

            if (resource != AvaloniaProperty.UnsetValue)
            {
                return new InstancedBinding(resource, Priority);
            }
            else
            {
                return null;
            }
        }
    }
}
