﻿#nullable enable

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents a <see cref="MenuItem"/>.
    /// </summary>
    public interface IMenuItem : IMenuElement
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
        new IMenuElement? Parent { get; }

        /// <summary>
        /// Raises a click event on the menu item.
        /// </summary>
        void RaiseClick();
    }
}
