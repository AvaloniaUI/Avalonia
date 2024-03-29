using System;
using System.Runtime.ConstrainedExecution;
using System.Threading;
using Avalonia.Utilities;

namespace Avalonia.Threading
{
    /// <summary>
    /// SynchronizationContext to be used on main thread
    /// </summary>
    public class AvaloniaSynchronizationContext : SynchronizationContext
    {
        internal readonly DispatcherPriority Priority;
        private readonly NonPumpingLockHelper.IHelperImpl? _nonPumpingHelper =
            AvaloniaLocator.Current.GetService<NonPumpingLockHelper.IHelperImpl>();
        private readonly Dispatcher _dispatcher;

        // This constructor is here to enforce STA behavior for unit tests
        internal AvaloniaSynchronizationContext(Dispatcher dispatcher, DispatcherPriority priority, bool isStaThread = false)
        {
            _dispatcher = dispatcher;
            Priority = priority;
            if (_nonPumpingHelper != null 
                && isStaThread)
                SetWaitNotificationRequired();
        }

        public AvaloniaSynchronizationContext()
            : this(Dispatcher.UIThread, DispatcherPriority.Default, Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
        {
        }

        public AvaloniaSynchronizationContext(DispatcherPriority priority)
            : this(Dispatcher.UIThread, priority, false)
        {
        }

        public AvaloniaSynchronizationContext(Dispatcher dispatcher, DispatcherPriority priority)
            : this(dispatcher, priority, false)
        {
        }

        /// <summary>
        /// Controls if SynchronizationContext should be installed in InstallIfNeeded. Used by Designer.
        /// </summary>
        public static bool AutoInstall { get; set; } = true;

        /// <summary>
        /// Installs synchronization context in current thread
        /// </summary>
        public static void InstallIfNeeded()
        {
            if (!AutoInstall || Current is AvaloniaSynchronizationContext)
            {
                return;
            }

            SetSynchronizationContext(Dispatcher.UIThread.GetContextWithPriority(DispatcherPriority.Normal));
        }

        /// <inheritdoc/>
        public override void Post(SendOrPostCallback d, object? state)
        {
            _dispatcher.Post(d, state, Priority);
        }

        /// <inheritdoc/>
        public override void Send(SendOrPostCallback d, object? state)
        {
            if (_dispatcher.CheckAccess())
                // Same-thread, use send priority to avoid any reentrancy.
                _dispatcher.Send(d, state, DispatcherPriority.Send);
            else
                _dispatcher.Send(d, state, Priority);
        }

#if !NET6_0_OR_GREATER
        [PrePrepareMethod]
#endif
        public override int Wait(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout)
        {
            if (
                _nonPumpingHelper != null
                && _dispatcher.CheckAccess() 
                && _dispatcher.DisabledProcessingCount > 0)
                return _nonPumpingHelper.Wait(waitHandles, waitAll, millisecondsTimeout);
            return base.Wait(waitHandles, waitAll, millisecondsTimeout);
        }

        public record struct RestoreContext : IDisposable
        {
            private readonly SynchronizationContext? _oldContext;
            private bool _needRestore;

            internal RestoreContext(SynchronizationContext? oldContext)
            {
                _oldContext = oldContext;
                _needRestore = true;
            }
            
            public void Dispose()
            {
                if (_needRestore)
                {
                    SetSynchronizationContext(_oldContext);
                    _needRestore = false;
                }
            }
        }

        public static RestoreContext Ensure(DispatcherPriority priority) => Ensure(Dispatcher.UIThread, priority);
        public static RestoreContext Ensure(Dispatcher dispatcher, DispatcherPriority priority)
        {
            if (Current is AvaloniaSynchronizationContext avaloniaContext 
                && avaloniaContext.Priority == priority)
                return default;
            var oldContext = Current;
            dispatcher.VerifyAccess();
            SetSynchronizationContext(dispatcher.GetContextWithPriority(priority));
            return new RestoreContext(oldContext);
        }
    }
}
