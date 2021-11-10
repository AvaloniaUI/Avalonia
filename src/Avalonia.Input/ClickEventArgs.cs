using System;
using Avalonia.Interactivity;

namespace Avalonia.Input
{
    public class ClickEventArgs : RoutedEventArgs
    {
        public EventArgs TriggerEventArgs { get; set; }

        public ClickEventArgs()
        {
        }

        public ClickEventArgs(RoutedEvent? routedEvent)
            : base(routedEvent)
        {
        }
        
        public ClickEventArgs(EventArgs triggerEventArgs)
        {
            TriggerEventArgs = triggerEventArgs;
        }

        public ClickEventArgs(RoutedEvent? routedEvent, EventArgs triggerEventArgs)
            : this(routedEvent)
        {
            TriggerEventArgs = triggerEventArgs;
        }
    }
}
