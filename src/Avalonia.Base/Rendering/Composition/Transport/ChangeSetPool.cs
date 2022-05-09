using System;
using System.Collections.Concurrent;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition.Transport
{
    interface IChangeSetPool
    {
        void Return(ChangeSet changes);
        ChangeSet Get(ServerObject target, Batch batch);
    }
    
    class ChangeSetPool<T> : IChangeSetPool where T : ChangeSet
    {
        private readonly Func<IChangeSetPool, T> _factory;
        private readonly ConcurrentBag<T> _pool = new ConcurrentBag<T>();

        public ChangeSetPool(Func<IChangeSetPool, T> factory)
        {
            _factory = factory;
        }

        public void Return(T changes)
        {
            changes.Reset();
            _pool.Add(changes);
        }

        void IChangeSetPool.Return(ChangeSet changes) => Return((T) changes);
        ChangeSet IChangeSetPool.Get(ServerObject target, Batch batch) => Get(target, batch);
        
        public T Get(ServerObject target, Batch batch)
        {
            if (!_pool.TryTake(out var res))
                res = _factory(this);
            res.Target = target;
            res.Batch = batch;
            res.Dispose = false;
            return res;
        }
    }
}