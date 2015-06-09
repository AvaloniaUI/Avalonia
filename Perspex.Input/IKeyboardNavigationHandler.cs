// -----------------------------------------------------------------------
// <copyright file="IKeyboardNavigationHandler.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input
{
    /// <summary>
    /// Defines the interface for classes that handle keyboard navigation for a window.
    /// </summary>
    public interface IKeyboardNavigationHandler
    {
        /// <summary>
        /// Sets the owner of the keyboard navigation handler.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <remarks>
        /// This method can only be called once, typically by the owner itself on creation.
        /// </remarks>
        void SetOwner(IInputRoot owner);

        /// <summary>
        /// Moves the focus to the next control in tab order.
        /// </summary>
        /// <param name="element">The current element.</param>
        void TabNext(IInputElement element);

        /// <summary>
        /// Moves the focus to the previous control in tab order.
        /// </summary>
        /// <param name="element">The current element.</param>
        void TabPrevious(IInputElement element);
    }
}