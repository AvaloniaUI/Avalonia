// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Perspex.Styling
{
    public static class StyleExtensions
    {
        /// <summary>
        /// Tries to find a named style resource.
        /// </summary>
        /// <param name="control">The control from which to find the resource.</param>
        /// <param name="name">The resource name.</param>
        /// <returns>
        /// The resource if found, otherwise <see cref="PerspexProperty.UnsetValue"/>.
        /// </returns>
        public static object FindStyleResource(this IStyleHost control, string name)
        {
            Contract.Requires<ArgumentNullException>(control != null);
            Contract.Requires<ArgumentNullException>(name != null);
            Contract.Requires<ArgumentException>(!string.IsNullOrWhiteSpace(name));

            while (control != null)
            {
                var result = control.Styles.FindResource(name);

                if (result != PerspexProperty.UnsetValue)
                {
                    return result;
                }

                control = control.StylingParent;
            }

            return PerspexProperty.UnsetValue;
        }
    }
}
