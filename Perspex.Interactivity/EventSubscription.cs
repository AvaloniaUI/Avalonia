// -----------------------------------------------------------------------
// <copyright file="EventSubscription.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

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
