using System;
using System.Threading;
using Avalonia.Utilities;

namespace Avalonia.Threading
{
    /// <summary>
    /// A <see cref="SynchronizationContext"/> that uses a <see cref="Dispatcher"/> to post messages.
    /// </summary>
    public class AvaloniaSynchronizationContext : SynchronizationContext
    {
        internal readonly DispatcherPriority Priority;
        private readonly NonPumpingLockHelper.IHelperImpl? _nonPumpingHelper =
            AvaloniaLocator.Current.GetService<NonPumpingLockHelper.IHelperImpl>();
        private readonly Dispatcher _dispatcher;

        // This constructor is here to enforce STA behavior for unit tests
        internal AvaloniaSynchronizationContext(Dispatcher dispatcher, DispatcherPriority priority, bool isStaThread)
        {
            _dispatcher = dispatcher;
            Priority = priority;
            if (_nonPumpingHelper != null 
                && isStaThread)
                SetWaitNotificationRequired();
        }

        public AvaloniaSynchronizationContext()
            : this(Dispatcher.CurrentDispatcher, DispatcherPriority.Default)
        {
        }

        public AvaloniaSynchronizationContext(DispatcherPriority priority)
            : this(Dispatcher.CurrentDispatcher, priority)
        {
        }

        public AvaloniaSynchronizationContext(Dispatcher dispatcher, DispatcherPriority priority)
            : this(dispatcher, priority, dispatcher.IsSta)
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

            SetSynchronizationContext(Dispatcher.CurrentDispatcher.GetContextWithPriority(DispatcherPriority.Normal));
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

        public static RestoreContext Ensure(DispatcherPriority priority) => Ensure(Dispatcher.CurrentDispatcher, priority);
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
