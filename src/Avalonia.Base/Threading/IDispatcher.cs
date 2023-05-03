using System;
using System.Threading;
using System.Threading.Tasks;

namespace Avalonia.Threading
{
    /// <summary>
    /// Dispatches jobs to a thread.
    /// </summary>
    public interface IDispatcher
    {
        /// <summary>
        /// Determines whether the calling thread is the thread associated with this <see cref="IDispatcher"/>.
        /// </summary>
        /// <returns>True if he calling thread is the thread associated with the dispatched, otherwise false.</returns>
        bool CheckAccess();

        /// <summary>
        /// Throws an exception if the calling thread is not the thread associated with this <see cref="IDispatcher"/>.
        /// </summary>
        void VerifyAccess();

        /// <summary>
        /// Posts an action that will be invoked on the dispatcher thread.
        /// </summary>
        /// <param name="action">The method.</param>
        /// <param name="priority">The priority with which to invoke the method.</param>
        void Post(Action action, DispatcherPriority priority = default);
    }
}
