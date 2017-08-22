// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls;

namespace Avalonia.Styling
{
    /// <summary>
    /// A style that consists of a number of child styles.
    /// </summary>
    public class Styles : AvaloniaList<IStyle>, IStyle
    {
        private IResourceDictionary _resources;

        /// <summary>
        /// Gets or sets a dictionary of style resources.
        /// </summary>
        public IResourceDictionary Resources
        {
            get
            {
                if (_resources == null)
                {
                    _resources = new ResourceDictionary();
                }

                return _resources;
            }
        }

        /// <summary>
        /// Attaches the style to a control if the style's selector matches.
        /// </summary>
        /// <param name="control">The control to attach to.</param>
        /// <param name="container">
        /// The control that contains this style. May be null.
        /// </param>
        public void Attach(IStyleable control, IStyleHost container)
        {
            foreach (IStyle style in this)
            {
                style.Attach(control, container);
            }
        }

        /// <inheritdoc/>
        public bool TryGetResource(string key, out object value)
        {
            for (var i = Count - 1; i >= 0; --i)
            {
                if (this[i].TryGetResource(key, out value))
                {
                    return true;
                }
            }

            value = null;
            return false;
        }
    }
}
