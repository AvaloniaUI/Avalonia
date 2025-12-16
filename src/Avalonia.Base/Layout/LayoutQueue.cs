using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia.Logging;

namespace Avalonia.Layout
{
    /// <summary>
    /// A specialized queue for layout operations that uses inline flags on Layoutable
    /// to avoid dictionary allocations for tracking queue state.
    /// </summary>
    internal sealed class LayoutQueue : IReadOnlyCollection<Layoutable>, IDisposable
    {
        private readonly Queue<Layoutable> _inner = new Queue<Layoutable>();
        private readonly List<Layoutable> _notFinalizedBuffer = new List<Layoutable>();
        private readonly HashSet<Layoutable> _seenInLoop = new HashSet<Layoutable>();
        private readonly bool _isMeasureQueue;

        private int _maxEnqueueCountPerLoop = 1;

        /// <summary>
        /// Creates a new LayoutQueue.
        /// </summary>
        /// <param name="isMeasureQueue">True for measure queue, false for arrange queue.</param>
        public LayoutQueue(bool isMeasureQueue)
        {
            _isMeasureQueue = isMeasureQueue;
        }

        public int Count => _inner.Count;

        public IEnumerator<Layoutable> GetEnumerator() => _inner.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _inner.GetEnumerator();

        public Layoutable Dequeue()
        {
            var result = _inner.Dequeue();

            // Use inline flags instead of dictionary lookup
            if (_isMeasureQueue)
            {
                result.IsInMeasureQueue = false;
            }
            else
            {
                result.IsInArrangeQueue = false;
            }

            return result;
        }

        public void Enqueue(Layoutable item)
        {
            // Use inline flags instead of dictionary lookup
            bool isActive;
            int count;
            
            if (_isMeasureQueue)
            {
                isActive = item.IsInMeasureQueue;
                count = item.MeasureQueueCount;
            }
            else
            {
                isActive = item.IsInArrangeQueue;
                count = item.ArrangeQueueCount;
            }

            if (!isActive)
            {
                if (count < _maxEnqueueCountPerLoop)
                {
                    _inner.Enqueue(item);
                    _seenInLoop.Add(item);
                    
                    if (_isMeasureQueue)
                    {
                        item.IsInMeasureQueue = true;
                        item.MeasureQueueCount = (byte)(count + 1);
                    }
                    else
                    {
                        item.IsInArrangeQueue = true;
                        item.ArrangeQueueCount = (byte)(count + 1);
                    }
                }
                else
                {
                    // Track items that hit the cycle limit for later processing in EndLoop
                    _seenInLoop.Add(item);
                    Logger.TryGet(LogEventLevel.Warning, LogArea.Layout)?.Log(
                        this,
                        "Layout cycle detected. Item {Item} was enqueued {Count} times.",
                        item,
                        count);
                }
            }
        }

        public void BeginLoop(int maxEnqueueCountPerLoop)
        {
            _maxEnqueueCountPerLoop = maxEnqueueCountPerLoop;
        }

        public void EndLoop()
        {
            // Collect items that hit the cycle limit from all seen items during the loop
            foreach (var item in _seenInLoop)
            {
                var count = _isMeasureQueue ? item.MeasureQueueCount : item.ArrangeQueueCount;
                if (count >= _maxEnqueueCountPerLoop)
                {
                    _notFinalizedBuffer.Add(item);
                }
            }

            // Reset counts for all items that were seen during the loop
            foreach (var item in _seenInLoop)
            {
                if (_isMeasureQueue)
                {
                    item.MeasureQueueCount = 0;
                }
                else
                {
                    item.ArrangeQueueCount = 0;
                }
            }

            _seenInLoop.Clear();

            // Prevent layout cycle but add to next layout the non arranged/measured items that might have caused cycle
            // one more time as a final attempt.
            foreach (var item in _notFinalizedBuffer)
            {
                bool shouldEnqueue = _isMeasureQueue ? !item.IsMeasureValid : !item.IsArrangeValid;
                if (shouldEnqueue)
                {
                    if (_isMeasureQueue)
                    {
                        item.IsInMeasureQueue = true;
                        item.MeasureQueueCount = 1;
                    }
                    else
                    {
                        item.IsInArrangeQueue = true;
                        item.ArrangeQueueCount = 1;
                    }
                    _inner.Enqueue(item);
                }
            }

            _notFinalizedBuffer.Clear();
        }

        public void Dispose()
        {
            // Reset inline flags for remaining items
            foreach (var item in _inner)
            {
                if (_isMeasureQueue)
                {
                    item.IsInMeasureQueue = false;
                    item.MeasureQueueCount = 0;
                }
                else
                {
                    item.IsInArrangeQueue = false;
                    item.ArrangeQueueCount = 0;
                }
            }
            
            _inner.Clear();
            _seenInLoop.Clear();
            _notFinalizedBuffer.Clear();
        }
    }
}
