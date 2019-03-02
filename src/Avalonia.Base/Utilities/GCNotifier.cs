// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Utilities
{
    public class GCNotifier
    {
        public static event EventHandler GarbageCollected;

        static GCNotifier()
        {
            new GCNotifier();
        }

        ~GCNotifier()
        {
            if (Environment.HasShutdownStarted || AppDomain.CurrentDomain.IsFinalizingForUnload())
            {
                return;
            }

            new GCNotifier();

            GarbageCollected?.Invoke(null, EventArgs.Empty);
        }
    }
}
