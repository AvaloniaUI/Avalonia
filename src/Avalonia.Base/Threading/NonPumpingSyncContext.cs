using System;
using System.Runtime.ConstrainedExecution;
using System.Threading;
using Avalonia.Utilities;

namespace Avalonia.Threading
{
    internal class NonPumpingSyncContext : SynchronizationContext, IDisposable
    {
        private readonly NonPumpingLockHelper.IHelperImpl _impl;
        private readonly SynchronizationContext? _inner;

        public NonPumpingSyncContext(NonPumpingLockHelper.IHelperImpl impl, SynchronizationContext? inner)
        {
            _impl = impl;
            _inner = inner;
            SetWaitNotificationRequired();
            SetSynchronizationContext(this);
        }

        public override void Post(SendOrPostCallback d, object? state)
        {
            if (_inner is null)
            {
#if NET6_0_OR_GREATER
                ThreadPool.QueueUserWorkItem(static x => x.d(x.state), (d, state), false);
#else
                ThreadPool.QueueUserWorkItem(_ => d(state));
#endif
            }
            else
            {
                _inner.Post(d, state);
            }
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
            if (_inner is null)
            {
                d(state);
            }
            else
            {
                _inner.Send(d, state);
            }
        }

#if !NET6_0_OR_GREATER
        [PrePrepareMethod]
#endif
        public override int Wait(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout) =>
            _impl.Wait(waitHandles, waitAll, millisecondsTimeout);

        public void Dispose() => SetSynchronizationContext(_inner);

        internal static IDisposable? Use(NonPumpingLockHelper.IHelperImpl impl)
        {
            var current = Current;
            if (current == null)
            {
                if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
                    return null;
            }
            if (current is NonPumpingSyncContext)
                return null;

            return new NonPumpingSyncContext(impl, current);
        }
        
    }
}
