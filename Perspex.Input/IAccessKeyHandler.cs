// -----------------------------------------------------------------------
// <copyright file="IAccessKeyHandler.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input
{
    /// <summary>
    /// Defines the interface for classes that handle access keys for a window.
    /// </summary>
    public interface IAccessKeyHandler
    {
        /// <summary>
        /// Gets or sets the window's main menu.
        /// </summary>
        IMainMenu MainMenu { get; set; }

        /// <summary>
        /// Sets the owner of the access key handler.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <remarks>
        /// This method can only be called once, typically by the owner itself on creation.
        /// </remarks>
        void SetOwner(IInputRoot owner);
    }
}
