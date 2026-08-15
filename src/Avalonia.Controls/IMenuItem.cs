namespace Avalonia.Controls
{
    /// <summary>
    /// Represents a <see cref="MenuItem"/>.
    /// </summary>
    internal interface IMenuItem : IMenuElement
    {
        /// <inheritdoc cref="MenuItem.HasSubMenu"/>
        bool HasSubMenu { get; }

        /// <summary>
        /// Gets a value indicating whether the mouse is currently over the menu item's submenu.
        /// </summary>
        bool IsPointerOverSubMenu { get; }

        /// <inheritdoc cref="MenuItem.IsSubMenuOpen"/>
        bool IsSubMenuOpen { get; set; }

        /// <inheritdoc cref="MenuItem.StaysOpenOnClick"/>
        bool StaysOpenOnClick { get; set; }

        /// <inheritdoc cref="MenuItem.IsTopLevel"/>
        bool IsTopLevel { get; }

        /// <summary>
        /// Gets the parent <see cref="IMenuElement"/>.
        /// </summary>
        IMenuElement? Parent { get; }

        /// <inheritdoc cref="MenuItem.ToggleType"/>
        MenuItemToggleType ToggleType { get; }

        /// <inheritdoc cref="MenuItem.GroupName"/>
        string? GroupName { get; }

        /// <inheritdoc cref="MenuItem.IsChecked"/>
        bool IsChecked { get; set; }

        /// <summary>
        /// Raises a click event on the menu item.
        /// </summary>
        void RaiseClick();
    }
}
