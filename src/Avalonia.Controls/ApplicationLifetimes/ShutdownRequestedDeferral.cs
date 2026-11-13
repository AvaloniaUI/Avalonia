using System;
using System.Threading;

namespace Avalonia.Controls.ApplicationLifetimes
{
    /// <summary>
    /// Represents asynchronous work that must complete before a native macOS shutdown request is handled.
    /// </summary>
    public sealed class ShutdownRequestedDeferral
    {
        private readonly Action<bool> _complete;
        private int _isCompleted;

        internal ShutdownRequestedDeferral(Action<bool> complete)
        {
            _complete = complete;
        }

        /// <summary>
        /// Completes the deferral.
        /// </summary>
        /// <param name="cancelShutdown">
        /// Set to <see langword="true"/> to cancel the shutdown request.
        /// </param>
        /// <exception cref="InvalidOperationException">The deferral has already been completed.</exception>
        public void Complete(bool cancelShutdown = false)
        {
            if (Interlocked.Exchange(ref _isCompleted, 1) != 0)
                throw new InvalidOperationException("The shutdown deferral has already been completed.");

            _complete(cancelShutdown);
        }
    }
}
