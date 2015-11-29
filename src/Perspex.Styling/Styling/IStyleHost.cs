// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Perspex.Styling
{
    /// <summary>
    /// Defines an element that has a <see cref="Styles"/> collection.
    /// </summary>
    public interface IStyleHost : IVisual
    {
        /// <summary>
        /// Gets the styles for the element.
        /// </summary>
        Styles Styles { get; }

        /// <summary>
        /// Gets the parent style host element.
        /// </summary>
        IStyleHost StylingParent { get; }

    }
}
