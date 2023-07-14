using System;
using System.Threading;
using System.Threading.Tasks;

namespace Avalonia.Threading;

public class DispatcherTaskAwaitable
{
    private readonly Task _task;
    private readonly DispatcherPriority _priority;

    internal DispatcherTaskAwaitable(Task task, DispatcherPriority priority)
    {
        _task = task;
        _priority = priority;
    }
            
    public void OnCompleted(Action continuation) =>
        _task.ContinueWith(delegate
            {
                Dispatcher.UIThread.Post(continuation, _priority);
            }, continuation, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);


    public bool IsCompleted => _task.IsCompleted;
    public void GetResult() => _task.GetAwaiter().GetResult();
    public DispatcherTaskAwaitable GetAwaiter() => this;
}
