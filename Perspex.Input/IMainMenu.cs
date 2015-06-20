// -----------------------------------------------------------------------
// <copyright file="IMainMenu.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input
{
    /// <summary>
    /// Defines the interface for a window's main menu.
    /// </summary>
    public interface IMainMenu : IVisual
    {
        /// <summary>
        /// Gets a value indicating whether the menu is open.
        /// </summary>
        bool IsOpen { get; }

        /// <summary>
        /// Closes the menu.
        /// </summary>
        void Close();

        /// <summary>
        /// Opens the menu in response to the Alt/F10 key.
        /// </summary>
        void Open();
    }
}
