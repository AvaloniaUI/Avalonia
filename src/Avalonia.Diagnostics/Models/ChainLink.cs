using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Interactivity;

namespace Avalonia.Diagnostics.Models
{
    internal class ChainLink
    {
        public object Handler { get; private set; }
        public string HandlerName
        {
            get
            {
                if (Handler is INamed named && !string.IsNullOrEmpty(named.Name))
                {
                    return named.Name + " (" + Handler.GetType().Name + ")";
                }
                return Handler.GetType().Name;
            }
        }
        public bool Handled { get; private set; }
        public RoutingStrategies Route { get; private set; }

        public ChainLink(object handler, bool handled, RoutingStrategies route)
        {
            Contract.Requires<ArgumentNullException>(handler != null);

            this.Handler = handler;
            this.Handled = handled;
            this.Route = route;
        }
    }
}
