using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Avalonia.Rendering.Composition.Transport
{
    /// <summary>
    /// Represents a group of serialized changes from the UI thread to be atomically applied at the render thread
    /// </summary>
    public class CompositionBatch
    {
        private static long _nextSequenceId = 1;
        private static ConcurrentBag<BatchStreamData> _pool = new();
        private readonly TaskCompletionSource<int> _committed = new();
        private readonly TaskCompletionSource<int> _acceptedTcs = new();
        private readonly TaskCompletionSource<int> _renderedTcs = new();
        
        internal long SequenceId { get; }
        
        public CompositionBatch()
        {
            SequenceId = Interlocked.Increment(ref _nextSequenceId);
            if (!_pool.TryTake(out var lst))
                lst = new BatchStreamData();
            Changes = lst;
        }

        
        internal BatchStreamData Changes { get; private set; }
        internal TimeSpan CommittedAt { get; set; }
        public Task Committed => _committed.Task;
        public Task Processed => _acceptedTcs.Task;
        public Task Rendered => _renderedTcs.Task;
        
        internal void NotifyProcessed()
        {
            _pool.Add(Changes);
            Changes = null!;

            _acceptedTcs.TrySetResult(0);
        }
        
        internal void NotifyRendered() => _renderedTcs.TrySetResult(0);
        internal void NotifyCommitted() => _committed.TrySetResult(0);
    }
}
