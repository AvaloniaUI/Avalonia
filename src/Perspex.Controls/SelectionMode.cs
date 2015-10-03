// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Perspex.Controls
{
    /// <summary>
    /// Defines the selection mode for a control which can select multiple items.
    /// </summary>
    public enum SelectionMode
    {
        /// <summary>
        /// One item can be selected at a time.
        /// </summary>
        Single,

        /// <summary>
        /// One item can be selected at a time, and there will always be a selected item as long
        /// as there are items to select.
        /// </summary>
        SingleAlways,

        /// <summary>
        /// Multiple items can be selected and their selection state is toggled by presses or by 
        /// pressing the spacebar.
        /// </summary>
        MultipleToggle,

        /// <summary>
        /// A range of items can be selected by holding the shift key, and individual items can be
        /// selected by holding the ctrl key.
        /// </summary>
        MultipleRange,
    }
}
