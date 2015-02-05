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
        public RoutedEventArgs()
        {
        }

        public RoutedEventArgs(RoutedEvent routedEvent, IInteractive source)
        {
            this.RoutedEvent = routedEvent;
            this.Source = this.OriginalSource = source;
        }

        public bool Handled { get; set; }

        public IInteractive OriginalSource { get; set; }

        public RoutedEvent RoutedEvent { get; set; }

        public RoutingStrategies Route { get; set; }

        public IInteractive Source { get; set; }
    }
}
