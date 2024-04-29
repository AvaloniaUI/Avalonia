using Avalonia.Metadata;

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents a <see cref="MenuItem"/>.
    /// </summary>
    internal interface IMenuItem : IMenuElement
    {
        /// <summary>
        /// Gets or sets a value that indicates whether the item has a submenu.
        /// </summary>
        bool HasSubMenu { get; }

        /// <summary>
        /// Gets a value indicating whether the mouse is currently over the menu item's submenu.
        /// </summary>
        bool IsPointerOverSubMenu { get; }

        /// <summary>
        /// Gets or sets a value that indicates whether the submenu of the <see cref="MenuItem"/> is
        /// open.
        /// </summary>
        bool IsSubMenuOpen { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates the submenu that this <see cref="MenuItem"/> is
        /// within should not close when this item is clicked.
        /// </summary>
        bool StaysOpenOnClick { get; set; }

        /// <summary>
        /// Gets a value that indicates whether the <see cref="MenuItem"/> is a top-level main menu item.
        /// </summary>
        bool IsTopLevel { get; }

        /// <summary>
        /// Gets the parent <see cref="IMenuElement"/>.
        /// </summary>
        IMenuElement? Parent { get; }

        /// <summary>
        /// Gets toggle type of the menu item.
        /// </summary>
        MenuItemToggleType ToggleType { get; }
        
        /// <summary>
        /// Gets menu item group name when <see cref="ToggleType"/> is <see cref="MenuItemToggleType.Radio"/>.
        /// </summary>
        string? GroupName { get; }
        
        /// <summary>
        /// Gets or sets if menu item is checked when <see cref="ToggleType"/> is
        /// <see cref="MenuItemToggleType.CheckBox"/> or <see cref="MenuItemToggleType.Radio"/>.
        /// </summary>
        bool IsChecked { get; set; }
        
        /// <summary>
        /// Raises a click event on the menu item.
        /// </summary>
        void RaiseClick();
    }
}
