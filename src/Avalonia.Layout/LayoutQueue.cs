using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Layout
{
    internal class LayoutQueue<T> : IReadOnlyCollection<T>
    {
        private struct Info
        {
            public bool Active;
            public int Count;
        }

        public LayoutQueue(Func<T, bool> shouldEnqueue)
        {
            _shouldEnqueue = shouldEnqueue;
        }

        private Func<T, bool> _shouldEnqueue;
        private Queue<T> _inner = new Queue<T>();
        private Dictionary<T, Info> _loopQueueInfo = new Dictionary<T, Info>();
        private int _maxEnqueueCountPerLoop = 1;

        public int Count => _inner.Count;

        public IEnumerator<T> GetEnumerator() => (_inner as IEnumerable<T>).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _inner.GetEnumerator();

        public T Dequeue()
        {
            var result = _inner.Dequeue();

            if (_loopQueueInfo.TryGetValue(result, out var info))
            {
                info.Active = false;
                _loopQueueInfo[result] = info;
            }

            return result;
        }

        public void Enqueue(T item)
        {
            _loopQueueInfo.TryGetValue(item, out var info);

            if (!info.Active && info.Count < _maxEnqueueCountPerLoop)
            {
                _inner.Enqueue(item);
                _loopQueueInfo[item] = new Info() { Active = true, Count = info.Count + 1 };
            }
        }

        public void BeginLoop(int maxEnqueueCountPerLoop)
        {
            _maxEnqueueCountPerLoop = maxEnqueueCountPerLoop;
        }

        public void EndLoop()
        {
            var notfinalized = _loopQueueInfo.Where(v => v.Value.Count == _maxEnqueueCountPerLoop).ToArray();

            _loopQueueInfo.Clear();

            //prevent layout cycle but add to next layout the non arranged/measured items that might have caused cycle
            //one more time as a final attempt
            foreach (var item in notfinalized)
            {
                if (_shouldEnqueue(item.Key))
                {
                    _loopQueueInfo[item.Key] = new Info() { Active = true, Count = item.Value.Count + 1 };
                    _inner.Enqueue(item.Key);
                }
            }
        }
    }
}
