using System;
using System.Runtime.ConstrainedExecution;
using System.Threading;
using Avalonia.Utilities;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32
{
    internal class NonPumpingSyncContext : SynchronizationContext, IDisposable
    {
        private readonly SynchronizationContext _inner;

        private NonPumpingSyncContext(SynchronizationContext inner)
        {
            _inner = inner;
            SetWaitNotificationRequired();
            SetSynchronizationContext(this);
        }

        public override void Post(SendOrPostCallback d, object? state) => _inner.Post(d, state);
        public override void Send(SendOrPostCallback d, object? state) => _inner.Send(d, state);

#if !NET6_0_OR_GREATER
        [PrePrepareMethod]
#endif
        public override int Wait(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout)
        {
            return UnmanagedMethods.WaitForMultipleObjectsEx(waitHandles.Length, waitHandles, waitAll,
                millisecondsTimeout, false);
        }

        public void Dispose() => SetSynchronizationContext(_inner);

        public static IDisposable? Use()
        {
            var current = Current;
            return current is null or NonPumpingSyncContext ? null : new NonPumpingSyncContext(current);
        }

        internal class HelperImpl : NonPumpingLockHelper.IHelperImpl
        {
            IDisposable? NonPumpingLockHelper.IHelperImpl.Use() => NonPumpingSyncContext.Use();
        }
    }
}
