// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Perspex.Collections;

namespace Perspex.Styling
{
    /// <summary>
    /// A style that consists of a number of child styles.
    /// </summary>
    public class Styles : PerspexList<IStyle>, IStyle
    {
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

        /// <summary>
        /// Tries to find a named resource within the style.
        /// </summary>
        /// <param name="name">The resource name.</param>
        /// <returns>
        /// The resource if found, otherwise <see cref="PerspexProperty.UnsetValue"/>.
        /// </returns>
        public object FindResource(string name)
        {
            foreach (var style in this.Reverse())
            {
                var result = style.FindResource(name);

                if (result != PerspexProperty.UnsetValue)
                {
                    return result;
                }
            }

            return PerspexProperty.UnsetValue;
        }
    }
}
