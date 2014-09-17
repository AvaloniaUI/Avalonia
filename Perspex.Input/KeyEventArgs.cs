// -----------------------------------------------------------------------
// <copyright file="PointerEventArgs.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input
{
    using System;
    using Perspex.Interactivity;

    public class KeyEventArgs : RoutedEventArgs
    {
        public IKeyboardDevice Device { get; set; }

        public Key Key { get; set; }

        public string Text { get; set; }
    }
}
