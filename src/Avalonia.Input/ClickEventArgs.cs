using System;
using Avalonia.Interactivity;

namespace Avalonia.Input
{
    public class ClickEventArgs : RoutedEventArgs
    {
        public EventArgs TriggeredEventArgs { get; set; }

        public ClickEventArgs()
        {
        }

        public ClickEventArgs(RoutedEvent? routedEvent)
            : base(routedEvent)
        {
        }
        
        public ClickEventArgs(EventArgs triggeredEventArgs)
        {
            TriggeredEventArgs = triggeredEventArgs;
        }

        public ClickEventArgs(RoutedEvent? routedEvent, EventArgs triggeredEventArgs)
            : this(routedEvent)
        {
            TriggeredEventArgs = triggeredEventArgs;
        }
    }
}
