using System;
using System.Runtime.ConstrainedExecution;
using System.Threading;
using Avalonia.Threading;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32
{
    internal class NonPumpingSyncContext : SynchronizationContext, IDisposable
    {
        private readonly SynchronizationContext _inner;

        private NonPumpingSyncContext()
        {
            _inner = Current;
            SetWaitNotificationRequired();
            SetSynchronizationContext(this);
        }

        public override void Post(SendOrPostCallback d, object state) => _inner.Post(d, state);
        public override void Send(SendOrPostCallback d, object state) => _inner.Send(d, state);
        
        [PrePrepareMethod]
        public override int Wait(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout)
        {
            return UnmanagedMethods.WaitForMultipleObjectsEx(waitHandles.Length, waitHandles, waitAll,
                millisecondsTimeout, false);
        }

        public void Dispose() => SynchronizationContext.SetSynchronizationContext(_inner);

        public static IDisposable Use() => new NonPumpingSyncContext();
    }
}
