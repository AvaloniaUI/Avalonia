using System;

namespace Avalonia.Controls
{
    /// <summary>
    /// Defines the selection mode for a control which can select multiple items.
    /// </summary>
    [Flags]
    public enum SelectionMode
    {
        /// <summary>
        /// One item can be selected.
        /// </summary>
        Single = 0x00,

        /// <summary>
        /// Multiple items can be selected.
        /// </summary>
        Multiple = 0x01,

        /// <summary>
        /// Item selection can be toggled by tapping/spacebar.
        /// </summary>
        Toggle = 0x02,

        /// <summary>
        /// An item will always be selected as long as there are items to select.
        /// </summary>
        AlwaysSelected = 0x04,
    }
}
