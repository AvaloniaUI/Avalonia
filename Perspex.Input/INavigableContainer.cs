// -----------------------------------------------------------------------
// <copyright file="INavigableContainer.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input
{
    /// <summary>
    /// Defines a container in which the child controls can be navigated by keyboard.
    /// </summary>
    public interface INavigableContainer
    {
        /// <summary>
        /// Gets the next control in the specified direction.
        /// </summary>
        /// <param name="direction">The movement direction.</param>
        /// <param name="from">The control from which movement begins.</param>
        /// <returns>The control.</returns>
        IInputElement GetControl(FocusNavigationDirection direction, IInputElement from);
    }
}
