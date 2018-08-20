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
        public bool CheckAccess()
        {
            return true;
        }

        public void Post(Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            action();
        }

        public Task InvokeAsync(Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            action();
            return Task.FromResult<object>(null);
        }

        public Task<TResult> InvokeAsync<TResult>(Func<TResult> function, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            var result = function();
            return Task.FromResult(result);
        }

        public void VerifyAccess()
        {
        }
    }
}
