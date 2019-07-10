using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Threading.Tasks;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.LogicalTree;
using Avalonia.Reactive;

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
            return new NeverEndingValueTaskObservable<object>(scope.FindAsync(name));
        }
        
        // This class is implemented in such weird way because for some reason
        // our binding system doesn't expect OnCompleted to be ever called and
        // seems to treat it as binding cancellation or something 

        private class NeverEndingValueTaskObservable<T> : IObservable<T>
        {
            private T _value;
            private Task<T> _task;

            public NeverEndingValueTaskObservable(ValueTask<T> task)
            {
                if (task.IsCompleted)
                    _value = task.Result;
                else
                    _task = task.AsTask();
            }
            
            public IDisposable Subscribe(IObserver<T> observer)
            {
                if (_task?.IsCompleted == true)
                {
                    _value = _task.Result;
                    _task = null;
                }

                if (_task != null)
                    // We expect everything to handle callbacks synchronously,
                    // so the object graph is ready after its built
                    // so keep TaskContinuationOptions.ExecuteSynchronously
                    _task.ContinueWith(t =>
                    {
                        observer.OnNext(t.Result);
                    }, TaskContinuationOptions.ExecuteSynchronously);
                else
                    observer.OnNext(_value);
                
                return Disposable.Empty;
            }
        }
    }
}
