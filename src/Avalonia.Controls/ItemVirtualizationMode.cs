// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Controls
{
    /// <summary>
    /// Describes the item virtualization method to use for a list.
    /// </summary>
    public enum ItemVirtualizationMode
    {
        /// <summary>
        /// Do not virtualize items.
        /// </summary>
        None,

        /// <summary>
        /// Virtualize items without smooth scrolling.
        /// </summary>
        Simple,
    }
}
