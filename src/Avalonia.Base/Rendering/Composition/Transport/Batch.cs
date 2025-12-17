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
    public sealed class CompositionBatch
    {
        private static long _nextSequenceId = 1;
        private static readonly ConcurrentBag<BatchStreamData> _dataPool = new();
        private static readonly ConcurrentBag<CompositionBatch> _batchPool = new();
        
        // Pre-warm the batch pool with a few instances to avoid initial allocations
        private const int InitialPoolSize = 4;
        
        static CompositionBatch()
        {
            for (var i = 0; i < InitialPoolSize; i++)
            {
                _batchPool.Add(new CompositionBatch(createForPool: true));
            }
        }
        
        private TaskCompletionSource<int> _acceptedTcs;
        private TaskCompletionSource<int> _renderedTcs;
        
        internal long SequenceId { get; private set; }
        
        /// <summary>
        /// Gets a CompositionBatch from the pool or creates a new one.
        /// </summary>
        internal static CompositionBatch Get()
        {
            if (!_batchPool.TryTake(out var batch))
                batch = new CompositionBatch(createForPool: false);
            
            batch.Initialize();
            return batch;
        }
        
        private CompositionBatch(bool createForPool)
        {
            _acceptedTcs = new TaskCompletionSource<int>();
            _renderedTcs = new TaskCompletionSource<int>();
            Changes = null!; // Will be set by Initialize()
            
            if (!createForPool)
                Initialize();
        }
        
        private void Initialize()
        {
            SequenceId = Interlocked.Increment(ref _nextSequenceId);
            if (!_dataPool.TryTake(out var lst))
                lst = new BatchStreamData();
            Changes = lst;
        }

        
        internal BatchStreamData Changes { get; private set; }
        internal TimeSpan CommittedAt { get; set; }
        
        /// <summary>
        /// Indicates that batch got deserialized on the render thread and will soon be rendered.
        /// It's generally a good time to start producing the next one
        /// </summary>
        /// <remarks>
        /// To allow timing-sensitive code to receive the notification in time, the TaskCompletionSource
        /// is configured to invoke continuations  _synchronously_, so your `await` could happen from the render loop
        /// if it happens to run on the UI thread.
        /// It's recommended to use Dispatcher.AwaitOnPriority when consuming from the UI thread 
        /// </remarks>
        public Task Processed => _acceptedTcs.Task;
        
        /// <summary>
        /// Indicates that batch got rendered on the render thread.
        /// It's generally a good time to start producing the next one
        /// </summary>
        /// <remarks>
        /// To allow timing-sensitive code to receive the notification in time, the TaskCompletionSource
        /// is configured to invoke continuations  _synchronously_, so your `await` could happen from the render loop
        /// if it happens to run on the UI thread.
        /// It's recommended to use Dispatcher.AwaitOnPriority when consuming from the UI thread 
        /// </remarks>
        public Task Rendered => _renderedTcs.Task;
        
        internal void NotifyProcessed()
        {
            _dataPool.Add(Changes);
            Changes = null!;

            _acceptedTcs.TrySetResult(0);
        }
        
        internal void NotifyRendered()
        {
            _renderedTcs.TrySetResult(0);
            
            // Return this batch to the pool for reuse
            ReturnToPool();
        }
        
        private void ReturnToPool()
        {
            // Reset the TaskCompletionSources for reuse
            // We need to create new ones since TCS can only be completed once
            _acceptedTcs = new TaskCompletionSource<int>();
            _renderedTcs = new TaskCompletionSource<int>();
            
            _batchPool.Add(this);
        }
    }
}
