using System;
using System.Runtime.ConstrainedExecution;
using System.Threading;

namespace Avalonia.Threading
{
    /// <summary>
    /// SynchronizationContext to be used on main thread
    /// </summary>
    public class AvaloniaSynchronizationContext : SynchronizationContext
    {
        public interface INonPumpingPlatformWaitProvider
        {
            int Wait(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout);
        }

        private readonly INonPumpingPlatformWaitProvider _waitProvider;

        public AvaloniaSynchronizationContext(INonPumpingPlatformWaitProvider waitProvider)
        {
            _waitProvider = waitProvider;
            if (_waitProvider != null)
                SetWaitNotificationRequired();
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

            SetSynchronizationContext(new AvaloniaSynchronizationContext(AvaloniaLocator.Current
                .GetService<INonPumpingPlatformWaitProvider>()));
        }

        /// <inheritdoc/>
        public override void Post(SendOrPostCallback d, object state)
        {
           Dispatcher.UIThread.Post(() => d(state), DispatcherPriority.Send);
        }

        /// <inheritdoc/>
        public override void Send(SendOrPostCallback d, object state)
        {
            if (Dispatcher.UIThread.CheckAccess())
                d(state);
            else
                Dispatcher.UIThread.InvokeAsync(() => d(state), DispatcherPriority.Send).Wait();
        }

        [PrePrepareMethod]
        public override int Wait(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout)
        {
            if (_waitProvider != null)
                return _waitProvider.Wait(waitHandles, waitAll, millisecondsTimeout);
            return base.Wait(waitHandles, waitAll, millisecondsTimeout);
        }
    }
}
