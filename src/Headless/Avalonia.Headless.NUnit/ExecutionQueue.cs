using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace Avalonia.Headless.NUnit;

// To force all tests (which might run in parallel) to be executed in a queue.
internal static class ExecutionQueue
{
    private static bool s_running;
    private static Queue<Func<Task>> s_queue = new();

    private static async void TryExecuteNext()
    {
        if (s_running || s_queue.Count == 0) return;
        try
        {
            s_running = true;
            await s_queue.Dequeue()();
        }
        finally
        {
            s_running = false;
        }
        TryExecuteNext();
    }

    private static void PostToTheQueue(this Dispatcher dispatcher, Func<Task> cb)
    {
        dispatcher.Post(() =>
        {
            s_queue.Enqueue(cb);
            TryExecuteNext();
        });
    }

    internal static Task<TResult> ExecuteOnQueue<TResult>(this Dispatcher dispatcher, Func<Task<TResult>> cb)
    {
        var tcs = new TaskCompletionSource<TResult>();
        PostToTheQueue(dispatcher, async () =>
        {
            try
            {
                var result = await cb();
                tcs.TrySetResult(result);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });
        return tcs.Task;
    }

    public static TResult InvokeOnQueue<TResult>(this Dispatcher dispatcher, Func<Task<TResult>> cb, CancellationToken cancellationToken = default)
    {
        return dispatcher
            .InvokeAsync(() => ExecuteOnQueue(dispatcher, cb), DispatcherPriority.Normal, cancellationToken)
            .GetTask().Unwrap()
            .Result;
    }
    
    public static Task<TResult> InvokeOnQueueAsync<TResult>(this Dispatcher dispatcher, Func<Task<TResult>> cb, CancellationToken cancellationToken = default)
    {
        return dispatcher
            .InvokeAsync(() => ExecuteOnQueue(dispatcher, cb), DispatcherPriority.Normal, cancellationToken)
            .GetTask().Unwrap();
    }
}
