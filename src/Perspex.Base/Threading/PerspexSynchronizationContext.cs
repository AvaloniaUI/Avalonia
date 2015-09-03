using System.Threading;

namespace Perspex.Threading
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// SynchronizationContext to be used on main thread
    /// </summary>
    public class PerspexSynchronizationContext : SynchronizationContext
    {
        /// <summary>
        /// Controls if SynchronizationContext should be installed in InstallIfNeeded
        /// </summary>
        public static bool AutoInstall { get; set; } = true;

        /// <summary>
        /// Installs synchronization context in current thread
        /// </summary>
        public static void InstallIfNeeded()
        {
            if (!AutoInstall || Current is PerspexSynchronizationContext)
            {
                return;
            }

            SetSynchronizationContext(new PerspexSynchronizationContext());
        }

        /// <inheritdoc/>
        public override void Post(SendOrPostCallback d, object state)
        {
            Dispatcher.UIThread.Post(() => d(state));
        }

        /// <inheritdoc/>
        public override void Send(SendOrPostCallback d, object state)
        {
            // TODO: Add check for being on the main thread, we should invoke the method immediately in this case
            Dispatcher.UIThread.InvokeAsync(() => d(state)).Wait();
        }
    }
}