// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Input
{
    /// <summary>
    /// Describes how focus should be moved by directional or tab keys.
    /// </summary>
    public enum NavigationDirection
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

        /// <summary>
        /// Move the focus up a page.
        /// </summary>
        PageUp,

        /// <summary>
        /// Move the focus down a page.
        /// </summary>
        PageDown,
    }

    public static class NavigationDirectionExtensions
    {
        /// <summary>
        /// Checks whether a <see cref="NavigationDirection"/> represents a tab movement.
        /// </summary>
        /// <param name="direction">The direction.</param>
        /// <returns>
        /// True if the direction represents a tab movement (<see cref="NavigationDirection.Next"/>
        /// or <see cref="NavigationDirection.Previous"/>); otherwise false.
        /// </returns>
        public static bool IsTab(this NavigationDirection direction)
        {
            return direction switch
            {
                NavigationDirection.Next => true,
                NavigationDirection.Previous => true,
                _ => false,
            };
        }

        /// <summary>
        /// Checks whether a <see cref="NavigationDirection"/> represents a directional movement.
        /// </summary>
        /// <param name="direction">The direction.</param>
        /// <returns>
        /// True if the direction represents a directional movement (Up, Down, Left, Right).
        /// </returns>
        public static bool IsDirectional(this NavigationDirection direction)
        {
            return direction switch
            {
                NavigationDirection.Up => true,
                NavigationDirection.Down => true,
                NavigationDirection.Left => true,
                NavigationDirection.Right => true,
                _ => false,
            };
        }

        /// <summary>
        /// Checks whether a <see cref="NavigationDirection"/> represents a paging movement.
        /// </summary>
        /// <param name="direction">The direction.</param>
        /// <returns>
        /// True if the direction represents a paging movement (Page Up, Page Down).
        /// </returns>
        public static bool IsPaging(this NavigationDirection direction)
        {
            return direction switch
            {
                NavigationDirection.PageUp => true,
                NavigationDirection.PageDown => true,
                _ => false,
            };
        }

        /// <summary>
        /// Converts a keypress into a <see cref="NavigationDirection"/>.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="modifiers">The keyboard modifiers.</param>
        /// <returns>
        /// A <see cref="NavigationDirection"/> if the keypress represents a navigation keypress.
        /// </returns>
        public static NavigationDirection? ToNavigationDirection(
            this Key key,
            InputModifiers modifiers = InputModifiers.None)
        {
            switch (key)
            {
                case Key.Tab:
                    return (modifiers & InputModifiers.Shift) != 0 ?
                        NavigationDirection.Next : NavigationDirection.Previous;
                case Key.Up:
                    return NavigationDirection.Up;
                case Key.Down:
                    return NavigationDirection.Down;
                case Key.Left:
                    return NavigationDirection.Left;
                case Key.Right:
                    return NavigationDirection.Right;
                case Key.Home:
                    return NavigationDirection.First;
                case Key.End:
                    return NavigationDirection.Last;
                case Key.PageUp:
                    return NavigationDirection.PageUp;
                case Key.PageDown:
                    return NavigationDirection.PageDown;
                default:
                    return null;
            }
        }
    }
}
