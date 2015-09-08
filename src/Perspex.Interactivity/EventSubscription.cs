





namespace Perspex.Interactivity
{
    using System;

    internal class EventSubscription
    {
        public Delegate Handler { get; set; }

        public RoutingStrategies Routes { get; set; }

        public bool AlsoIfHandled { get; set; }
    }
}
