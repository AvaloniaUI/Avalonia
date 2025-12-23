using System;
using System.Collections.Generic;
using System.Threading;

namespace Avalonia.UnitTests
{
    public sealed class UnitTestSynchronizationContext : SynchronizationContext
    {
        private readonly List<(SendOrPostCallback callback, object? state)> _postedCallbacks = [];

        public static Scope Begin()
        {
            var sync = new UnitTestSynchronizationContext();
            var old = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(sync);
            return new Scope(old, sync);
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
            d(state);
        }

        public override void Post(SendOrPostCallback d, object? state)
        {
            lock (_postedCallbacks)
            {
                _postedCallbacks.Add((d, state));
            }
        }

        public void ExecutePostedCallbacks()
        {
            lock (_postedCallbacks)
            {
                _postedCallbacks.ForEach(t => t.callback(t.state));
                _postedCallbacks.Clear();
            }
        }

        public class Scope : IDisposable
        {
            private readonly SynchronizationContext? _old;
            private readonly UnitTestSynchronizationContext _new;

            public Scope(SynchronizationContext? old, UnitTestSynchronizationContext n)
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
