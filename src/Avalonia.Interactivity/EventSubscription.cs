// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Interactivity
{
    internal delegate void InvokeSignature(Delegate func, object sender, RoutedEventArgs args);

    internal class EventSubscription
    {
        public InvokeSignature RaiseHandler { get; set; }

        public Delegate Handler { get; set; }

        public RoutingStrategies Routes { get; set; }

        public bool AlsoIfHandled { get; set; }
    }
}
