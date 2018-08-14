using System;
using Avalonia.Input;

namespace Avalonia.Controls.Platform
{
    /// <summary>
    /// Handles user interaction for menus.
    /// </summary>
    public interface IMenuInteractionHandler
    {
        /// <summary>
        /// Attaches the interaction handler to a menu.
        /// </summary>
        /// <param name="menu">The menu.</param>
        void Attach(IMenu menu);

        /// <summary>
        /// Detaches the interaction handler from the attached menu.
        /// </summary>
        void Detach(IMenu menu);
    }
}
