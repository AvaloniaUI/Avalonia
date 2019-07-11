using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Threading.Tasks;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.LogicalTree;
using Avalonia.Reactive;
using Avalonia.Utilities;

namespace Avalonia.Controls
{
    public class NameScopeLocator
    {
        /// <summary>
        /// Tracks a named control relative to another control.
        /// </summary>
        /// <param name="relativeTo">
        /// The control relative from which the other control should be found.
        /// </param>
        /// <param name="name">The name of the control to find.</param>
        public static IObservable<object> Track(INameScope scope, string name)
        {
            return new NeverEndingSynchronousCompletionAsyncResultObservable<object>(scope.FindAsync(name));
        }
        
        // This class is implemented in such weird way because for some reason
        // our binding system doesn't expect OnCompleted to be ever called and
        // seems to treat it as binding cancellation or something 

        private class NeverEndingSynchronousCompletionAsyncResultObservable<T> : IObservable<T>
        {
            private T _value;
            private SynchronousCompletionAsyncResult<T>? _task;

            public NeverEndingSynchronousCompletionAsyncResultObservable(SynchronousCompletionAsyncResult<T> task)
            {
                if (task.IsCompleted)
                    _value = task.GetResult();
                else
                    _task = task;
            }
            
            public IDisposable Subscribe(IObserver<T> observer)
            {
                if (_task?.IsCompleted == true)
                {
                    _value = _task.Value.GetResult();
                    _task = null;
                }

                if (_task != null)
                    // We expect everything to handle callbacks synchronously,
                    // so the object graph is ready after its built
                    // so keep TaskContinuationOptions.ExecuteSynchronously
                    _task.Value.OnCompleted(() =>
                    {
                        observer.OnNext(_task.Value.GetResult());
                    });
                else
                    observer.OnNext(_value);
                
                return Disposable.Empty;
            }
        }
    }
}
