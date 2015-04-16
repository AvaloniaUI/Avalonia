// -----------------------------------------------------------------------
// <copyright file="GotFocusEventArgs.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input
{
    using Perspex.Interactivity;

    public class GotFocusEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Gets or sets a value indicating whether the control was focused by a keypress (e.g. 
        /// the Tab key).
        /// </summary>
        public bool KeyboardNavigated { get; set; }
    }
}
