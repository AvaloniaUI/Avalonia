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

        /// <summary>
        /// Virtualize items without smooth scrolling and recycle the containing controls.
        /// This mode wont work when you have items that can be in different states
        /// i.e. expander. see: https://github.com/AvaloniaUI/Avalonia/issues/1758
        /// </summary>
        Recycle
    }
}
