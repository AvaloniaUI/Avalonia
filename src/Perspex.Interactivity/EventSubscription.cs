// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Perspex.Interactivity
{
    internal class EventSubscription
    {
        public Delegate Handler { get; set; }

        public RoutingStrategies Routes { get; set; }

        public bool AlsoIfHandled { get; set; }
    }
}
