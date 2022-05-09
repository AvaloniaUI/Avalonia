using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Avalonia.Rendering.Composition.Transport
{
    internal class Batch
    {
        private static long _nextSequenceId = 1;
        private static ConcurrentBag<List<ChangeSet>> _pool = new ConcurrentBag<List<ChangeSet>>();
        public long SequenceId { get; }
        
        public Batch()
        {
            SequenceId = Interlocked.Increment(ref _nextSequenceId);
            if (!_pool.TryTake(out var lst))
                lst = new List<ChangeSet>();
            Changes = lst;
        }
        private TaskCompletionSource<int> _tcs = new TaskCompletionSource<int>();
        public List<ChangeSet> Changes { get; private set; }
        public TimeSpan CommitedAt { get; set; }
        
        public void Complete()
        {
            Changes.Clear();
            _pool.Add(Changes);
            Changes = null!;

            _tcs.TrySetResult(0);
        }

        public Task Completed => _tcs.Task;
    }
}