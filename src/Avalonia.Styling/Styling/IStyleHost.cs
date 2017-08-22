// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Styling
{
    /// <summary>
    /// Defines an element that has a <see cref="Styles"/> collection.
    /// </summary>
    public interface IStyleHost
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
