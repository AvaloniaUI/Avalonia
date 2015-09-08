





namespace Perspex.Interactivity
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

        public RoutedEventArgs(RoutedEvent routedEvent, IInteractive source)
        {
            this.RoutedEvent = routedEvent;
            this.Source = source;
        }

        public bool Handled { get; set; }

        public RoutedEvent RoutedEvent { get; set; }

        public RoutingStrategies Route { get; set; }

        public IInteractive Source { get; set; }
    }
}
