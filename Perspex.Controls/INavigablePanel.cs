// -----------------------------------------------------------------------
// <copyright file="INavigablePanel.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using Perspex.Input;

    /// <summary>
    /// Defines a panel in which the child controls can be navigated by keyboard.
    /// </summary>
    public interface INavigablePanel
    {
        /// <summary>
        /// Gets the next control in the specified direction.
        /// </summary>
        /// <param name="direction">The movement direction.</param>
        /// <param name="from">The control from which movement begins.</param>
        /// <returns>The control.</returns>
        IControl GetControl(FocusNavigationDirection direction, IControl from);
    }
}
