using System.Collections.Generic;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Metadata;

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents an <see cref="IMenu"/> or <see cref="IMenuItem"/>.
    /// </summary>
    internal interface IMenuElement : IInputElement, ILogical
    {
        /// <summary>
        /// Gets or sets the currently selected submenu item.
        /// </summary>
        IMenuItem? SelectedItem { get; set; }

        /// <summary>
        /// Gets the submenu items.
        /// </summary>
        IEnumerable<IMenuItem> SubItems { get; }

        /// <summary>
        /// Opens the menu or menu item.
        /// </summary>
        void Open();

        /// <summary>
        /// Closes the menu or menu item.
        /// </summary>
        void Close();

        /// <summary>
        /// Moves the submenu selection in the specified direction.
        /// </summary>
        /// <param name="direction">The direction.</param>
        /// <param name="wrap">Whether to wrap after the first or last item.</param>
        /// <returns>True if the selection was moved; otherwise false.</returns>
        bool MoveSelection(NavigationDirection direction, bool wrap);
    }
}
