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

            SetSynchronizationContext(new AvaloniaSynchronizationContext());
        }

        /// <inheritdoc/>
        public override void Post(SendOrPostCallback d, object? state)
        {
           Dispatcher.UIThread.Post(d, state, DispatcherPriority.Background);
        }

        /// <inheritdoc/>
        public override void Send(SendOrPostCallback d, object? state)
        {
            if (Dispatcher.UIThread.CheckAccess())
                d(state);
            else
                Dispatcher.UIThread.InvokeAsync(() => d(state), DispatcherPriority.Send).GetAwaiter().GetResult();
        }


    }
}
