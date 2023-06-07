using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// Special license applies <see href="https://raw.githubusercontent.com/AvaloniaUI/Avalonia/master/src/Avalonia.Base/Rendering/Composition/License.md">License.md</see>

namespace Avalonia.Rendering.Composition.Transport
{
    /// <summary>
    /// Represents a group of serialized changes from the UI thread to be atomically applied at the render thread
    /// </summary>
    internal class Batch
    {
        private static long _nextSequenceId = 1;
        private static ConcurrentBag<BatchStreamData> _pool = new();
        private readonly TaskCompletionSource<int> _acceptedTcs = new();
        private readonly TaskCompletionSource<int> _renderedTcs = new();
        
        public long SequenceId { get; }
        
        public Batch()
        {
            SequenceId = Interlocked.Increment(ref _nextSequenceId);
            if (!_pool.TryTake(out var lst))
                lst = new BatchStreamData();
            Changes = lst;
        }

        
        public BatchStreamData Changes { get; private set; }
        public TimeSpan CommittedAt { get; set; }
        public Task Processed => _acceptedTcs.Task;
        public Task Rendered => _renderedTcs.Task;
        
        public void NotifyProcessed()
        {
            _pool.Add(Changes);
            Changes = null!;

            _acceptedTcs.TrySetResult(0);
        }
        
        public void NotifyRendered() => _renderedTcs.TrySetResult(0);
    }
}
