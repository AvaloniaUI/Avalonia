// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Perspex.Controls;
using Perspex.Data;
using Perspex.Styling;

namespace Perspex.Markup.Xaml.Data
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
            IPerspexObject target,
            PerspexProperty targetProperty,
            object anchor = null)
        {
            var host = (target as IControl) ?? (anchor as IControl);
            var style = anchor as IStyle;
            var resource = PerspexProperty.UnsetValue;

            if (host != null)
            {
                resource = host.FindStyleResource(Name);
            }
            else if (style != null)
            {
                resource = style.FindResource(Name);
            }

            if (resource != PerspexProperty.UnsetValue)
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
