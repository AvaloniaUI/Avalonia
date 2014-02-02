// -----------------------------------------------------------------------
// <copyright file="RoutedEventArgs.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using System;

    public class RoutedEventArgs : EventArgs
    {
        public RoutedEventArgs()
        {
        }

        public RoutedEventArgs(RoutedEvent routedEvent)
        {
            this.RoutedEvent = routedEvent;
        }

        public RoutedEventArgs(RoutedEvent routedEvent, object source)
        {
            this.RoutedEvent = routedEvent;
            this.Source = source;
        }

        public bool Handled { get; set; }

        public object OriginalSource { get; set; }

        public RoutedEvent RoutedEvent { get; set; }

        public object Source { get; set; }
    }
}
