// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Threading;

namespace Avalonia.Markup.UnitTests
{
    internal sealed class UnitTestSynchronizationContext : SynchronizationContext
    {
        readonly List<Tuple<SendOrPostCallback, object>> _postedCallbacks =
            new List<Tuple<SendOrPostCallback, object>>();

        public static Scope Begin()
        {
            var sync = new UnitTestSynchronizationContext();
            var old = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(sync);
            return new Scope(old, sync);
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            d(state);
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            lock (_postedCallbacks)
            {
                _postedCallbacks.Add(Tuple.Create(d, state));
            }
        }

        public void ExecutePostedCallbacks()
        {
            lock (_postedCallbacks)
            {
                _postedCallbacks.ForEach(t => t.Item1(t.Item2));
                _postedCallbacks.Clear();
            }
        }

        public class Scope : IDisposable
        {
            private readonly SynchronizationContext _old;
            private readonly UnitTestSynchronizationContext _new;

            public Scope(SynchronizationContext old, UnitTestSynchronizationContext n)
            {
                _old = old;
                _new = n;
            }

            public void Dispose()
            {
                SynchronizationContext.SetSynchronizationContext(_old);
            }

            public void ExecutePostedCallbacks()
            {
                _new.ExecutePostedCallbacks();
            }
        }
    }
}
