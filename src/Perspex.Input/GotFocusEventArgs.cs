// -----------------------------------------------------------------------
// <copyright file="GotFocusEventArgs.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input
{
    using Perspex.Interactivity;

    /// <summary>
    /// Holds arguments for a <see cref="InputElement.GotFocusEvent"/>.
    /// </summary>
    public class GotFocusEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Gets or sets a value indicating how the change in focus occurred.
        /// </summary>
        public NavigationMethod NavigationMethod { get; set; }
    }
}
