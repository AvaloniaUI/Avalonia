// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Interactivity
{
    public class RoutedEventArgs : EventArgs
    {
        public RoutedEventArgs()
        {
        }

        public RoutedEventArgs(RoutedEvent routedEvent)
        {
            RoutedEvent = routedEvent;
        }

        public RoutedEventArgs(RoutedEvent routedEvent, IInteractive source)
        {
            RoutedEvent = routedEvent;
            Source = source;
        }

        public bool Handled { get; set; }

        public RoutedEvent RoutedEvent { get; set; }

        public RoutingStrategies Route { get; set; }

        public IInteractive Source { get; set; }
    }
}
