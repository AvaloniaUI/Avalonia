using System;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace Avalonia.UnitTests
{
    /// <summary>
    /// Immediately invokes dispatched jobs on the current thread.
    /// </summary>
    public class ImmediateDispatcher : IDispatcher
    {
        /// <inheritdoc/>
        public bool CheckAccess()
        {
            return true;
        }

        /// <inheritdoc/>
        public void Post(Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            action();
        }

        /// <inheritdoc/>
        public Task InvokeAsync(Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            action();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<TResult> InvokeAsync<TResult>(Func<TResult> function, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            var result = function();
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task InvokeAsync(Func<Task> function, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            return function();
        }
        
        /// <inheritdoc/>
        public Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> function, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            return function();
        }
        
        /// <inheritdoc/>
        public void VerifyAccess()
        {
        }
    }
}
