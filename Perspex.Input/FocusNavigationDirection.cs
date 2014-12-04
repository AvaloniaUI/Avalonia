// -----------------------------------------------------------------------
// <copyright file="FocusNavigationDirection.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input
{
    /// <summary>
    /// Describes how focus should be moved.
    /// </summary>
    public enum FocusNavigationDirection
    {
        /// <summary>
        /// Move the focus to the next control in the tab order.
        /// </summary>
        Next,

        /// <summary>
        /// Move the focus to the previous control in the tab order.
        /// </summary>
        Previous,

        /// <summary>
        /// Move the focus to the first control in the tab order.
        /// </summary>
        First,

        /// <summary>
        /// Move the focus to the last control in the tab order.
        /// </summary>
        Last,

        /// <summary>
        /// Move the focus to the left.
        /// </summary>
        Left,

        /// <summary>
        /// Move the focus to the right.
        /// </summary>
        Right,

        /// <summary>
        /// Move the focus up.
        /// </summary>
        Up,

        /// <summary>
        /// Move the focus down.
        /// </summary>
        Down,
    }
}
