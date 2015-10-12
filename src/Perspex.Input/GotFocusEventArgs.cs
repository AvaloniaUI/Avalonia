// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Interactivity;

namespace Perspex.Input
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
