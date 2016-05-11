// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Interactivity;

namespace Avalonia.Input
{
    /// <summary>
    /// Holds arguments for a <see cref="InputElement.GotFocusEvent"/>.
    /// </summary>
    public class GotFocusEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Gets or sets a value indicating how the change in focus occurred.
        /// </summary>
        public NavigationMethod NavigationMethod { get; set; }

        /// <summary>
        /// Gets or sets any input modifiers active at the time of focus.
        /// </summary>
        public InputModifiers InputModifiers { get; set; }
    }
}
