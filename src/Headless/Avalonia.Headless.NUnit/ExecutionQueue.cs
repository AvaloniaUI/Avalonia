using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace Avalonia.Headless.NUnit;

internal static class ExecutionQueue
{
    static bool _running;
    static Queue<Func<Task>> _queue = new();
    static async void TryExecuteNext()
    {
        if (_running || _queue.Count == 0) return;
        try
        {
            _running = true;
            await _queue.Dequeue()();
        }
        finally
        {
            _running = false;
        }
        TryExecuteNext();
    }

    static void ExecuteOnQueue(this Dispatcher dispatcher, Func<Task> cb)
    {
        dispatcher.Post(() =>
        {
            _queue.Enqueue(cb);
            TryExecuteNext();
        });
    }
}
