// -----------------------------------------------------------------------
// <copyright file="RoutedEventArgs.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Interactivity
{
    using System;

    public class RoutedEventArgs : EventArgs
    {
        public bool Handled { get; set; }

        public IInteractive OriginalSource { get; set; }

        public RoutedEvent RoutedEvent { get; set; }

        public IInteractive Source { get; set; }
    }
}
