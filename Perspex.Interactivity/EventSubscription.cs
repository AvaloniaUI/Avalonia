using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perspex.Interactivity
{
    internal class EventSubscription
    {
        public Delegate Handler { get; set; }

        public RoutingStrategies Routes { get; set; }

        public bool AlsoIfHandled { get; set; }
    }
}
