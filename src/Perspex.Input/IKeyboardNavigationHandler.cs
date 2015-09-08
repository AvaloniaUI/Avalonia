// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

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
        /// Moves the focus in the specified direction.
        /// </summary>
        /// <param name="element">The current element.</param>
        /// <param name="direction">The direction to move.</param>
        void Move(IInputElement element, FocusNavigationDirection direction);
    }
}