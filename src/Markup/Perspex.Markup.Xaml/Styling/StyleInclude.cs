// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Styling;

namespace Perspex.Markup.Xaml.Styling
{
    /// <summary>
    /// Includes a style from a URL.
    /// </summary>
    public class StyleInclude : IStyle
    {
        /// <summary>
        /// Gets or sets the source URL.
        /// </summary>
        public Uri Source { get; set; }

        /// <summary>
        /// Gets the loaded style.
        /// </summary>
        public IStyle Loaded { get; private set; }

        /// <inheritdoc/>
        public void Attach(IStyleable control, IStyleHost container)
        {
            if (Source != null)
            {
                if (Loaded == null)
                {
                    var loader = new PerspexXamlLoader();
                    Loaded = (IStyle)loader.Load(Source);
                }

                Loaded.Attach(control, container);
            }
        }

        /// <summary>
        /// Tries to find a named resource within the style.
        /// </summary>
        /// <param name="name">The resource name.</param>
        /// <returns>
        /// The resource if found, otherwise <see cref="PerspexProperty.UnsetValue"/>.
        /// </returns>
        public object FindResource(string name)
        {
            return Loaded.FindResource(name);
        }
    }
}
