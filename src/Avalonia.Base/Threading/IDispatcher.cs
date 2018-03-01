using System;
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
        /// Invokes a method on the dispatcher thread.
        /// </summary>
        /// <param name="action">The method.</param>
        /// <param name="priority">The priority with which to invoke the method.</param>
        /// <returns>A task that can be used to track the method's execution.</returns>
        void Post(Action action, DispatcherPriority priority = DispatcherPriority.Normal);

        /// <summary>
        /// Post action that will be invoked on main thread
        /// </summary>
        /// <param name="action">The method.</param>
        /// <param name="priority">The priority with which to invoke the method.</param>
        // TODO: The naming of this method is confusing: the Async suffix usually means return a task.
        // Remove this and rename InvokeTaskAsync as InvokeAsync. See #816.
        Task InvokeAsync(Action action, DispatcherPriority priority = DispatcherPriority.Normal);
    }
}