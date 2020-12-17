using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Avalonia.FreeDesktop
{
    class DBusCallQueue
    {
        class Item
        {
            public Func<Task> Callback;
            public Func<Exception, Task> OnFinish;
        }
        private Queue<Item> _q = new Queue<Item>();
        private bool _processing;
        
        public void Enqueue(Func<Task> cb, Func<Exception, Task> onError)
        {
            _q.Enqueue(new Item
            {
                Callback = cb,
                OnFinish = e =>
                {
                    if (e != null)
                        return onError?.Invoke(e);
                    return Task.CompletedTask;
                }
            });
            Process();
        }

        public Task EnqueueAsync(Func<Task> cb)
        {
            var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            _q.Enqueue(new Item
            {
                Callback = cb,
                OnFinish = e =>
                {
                    if (e == null)
                        tcs.TrySetResult(0);
                    else
                        tcs.TrySetException(e);
                    return Task.CompletedTask;
                }
            });
            Process();
            return tcs.Task;
        }
        
        public Task<T> EnqueueAsync<T>(Func<Task<T>> cb)
        {
            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            _q.Enqueue(new Item
            {
                Callback = async () =>
                {
                    var res = await cb();
                    tcs.TrySetResult(res);
                },
                OnFinish = e =>
                {
                    if (e != null)
                        tcs.TrySetException(e);
                    return Task.CompletedTask;
                }
            });
            Process();
            return tcs.Task;
        }

        async void Process()
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
                        await item.OnFinish(null);
                    }
                    catch(Exception e)
                    {
                        await item.OnFinish(e);
                    }
                }
            }
            finally
            {
                _processing = false;
            }
        }
    }
}
