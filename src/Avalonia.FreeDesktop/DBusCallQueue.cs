using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Avalonia.FreeDesktop
{
    class DBusCallQueue
    {
        private readonly Func<Exception, Task> _errorHandler;
        private readonly Queue<Item> _q = new();

        private bool _processing;

        private record Item(Func<Task> Callback)
        {
            public Action<Exception?>? OnFinish;
        }

        public DBusCallQueue(Func<Exception, Task> errorHandler)
        {
            _errorHandler = errorHandler;
        }

        public void Enqueue(Func<Task> cb)
        {
            _q.Enqueue(new Item(cb));
            Process();
        }

        public Task EnqueueAsync(Func<Task> cb)
        {
            var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            _q.Enqueue(new Item(cb)
            {
                OnFinish = e =>
                {
                    if (e == null)
                        tcs.TrySetResult(0);
                    else
                        tcs.TrySetException(e);
                }
            });
            Process();
            return tcs.Task;
        }

        public Task<T> EnqueueAsync<T>(Func<Task<T>> cb)
        {
            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            _q.Enqueue(new Item(async () =>
                {
                    var res = await cb();
                    tcs.TrySetResult(res);
                })
            {
                OnFinish = e =>
                {
                    if (e != null)
                        tcs.TrySetException(e);
                }
            });
            Process();
            return tcs.Task;
        }

        private async void Process()
        {
            if(_processing)
                return;
            _processing = true;
            try
            {
                while (_q.Count > 0)
                {
                    var item = _q.Dequeue();
                    try
                    {
                        await item.Callback();
                        item.OnFinish?.Invoke(null);
                    }
                    catch(Exception e)
                    {
                        if (item.OnFinish != null)
                            item.OnFinish(e);
                        else
                            await _errorHandler(e);
                    }
                }
            }
            finally
            {
                _processing = false;
            }
        }

        public void FailAll()
        {
            while (_q.Count>0)
            {
                var item = _q.Dequeue();
                item.OnFinish?.Invoke(new OperationCanceledException());
            }
        }
    }
}
