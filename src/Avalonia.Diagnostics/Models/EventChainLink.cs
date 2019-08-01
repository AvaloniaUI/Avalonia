// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Interactivity;

namespace Avalonia.Diagnostics.Models
{
    internal class EventChainLink
    {
        public EventChainLink(object handler, bool handled, RoutingStrategies route)
        {
            Contract.Requires<ArgumentNullException>(handler != null);

            Handler = handler;
            Handled = handled;
            Route = route;
        }

        public object Handler { get; }

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

        public bool Handled { get; }

        public RoutingStrategies Route { get; }
    }
}
