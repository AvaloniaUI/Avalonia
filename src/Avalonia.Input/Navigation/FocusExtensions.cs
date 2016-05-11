// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Input.Navigation
{
    /// <summary>
    /// Provides extension methods relating to control focus.
    /// </summary>
    internal static class FocusExtensions
    {
        /// <summary>
        /// Checks if the specified element can be focused.
        /// </summary>
        /// <param name="e">The element.</param>
        /// <returns>True if the element can be focused.</returns>
        public static bool CanFocus(this IInputElement e) => e.Focusable && e.IsEnabledCore && e.IsVisible;

        /// <summary>
        /// Checks if descendents of the specified element can be focused.
        /// </summary>
        /// <param name="e">The element.</param>
        /// <returns>True if descendents of the element can be focused.</returns>
        public static bool CanFocusDescendents(this IInputElement e) => e.IsEnabledCore && e.IsVisible;
    }
}
