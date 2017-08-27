// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Styling
{
    /// <summary>
    /// Defines an element that has a <see cref="Styles"/> collection.
    /// </summary>
    public interface IStyleHost
    {
        /// <summary>
        /// Gets a value indicating whether <see cref="Styles"/> is initialized.
        /// </summary>
        /// <remarks>
        /// The <see cref="Styles"/> property may be lazily initialized, if so this property
        /// indicates whether it has been initialized.
        /// </remarks>
        bool IsStylesInitialized { get; }

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
