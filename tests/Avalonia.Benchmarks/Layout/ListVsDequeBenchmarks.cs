using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Layout
{
    /// <summary>
    /// Benchmarks comparing List vs Deque for virtualization scenarios.
    /// Tests the O(n) vs O(1) insertion at beginning performance difference.
    /// </summary>
    [MemoryDiagnoser]
    public class ListVsDequeBenchmarks
    {
        /// <summary>
        /// Simple circular buffer deque implementation for benchmarking.
        /// </summary>
        private class Deque<T>
        {
            private T[] _buffer;
            private int _head;
            private int _count;

            public Deque(int capacity = 16)
            {
                _buffer = new T[capacity];
                _head = 0;
                _count = 0;
            }

            public int Count => _count;

            public void PushFront(T item)
            {
                EnsureCapacity();
                _head = (_head - 1 + _buffer.Length) % _buffer.Length;
                _buffer[_head] = item;
                _count++;
            }

            public void PushBack(T item)
            {
                EnsureCapacity();
                var tail = (_head + _count) % _buffer.Length;
                _buffer[tail] = item;
                _count++;
            }

            public T PopFront()
            {
                if (_count == 0)
                    throw new InvalidOperationException("Deque is empty");
                var item = _buffer[_head];
                _buffer[_head] = default!;
                _head = (_head + 1) % _buffer.Length;
                _count--;
                return item;
            }

            public T PopBack()
            {
                if (_count == 0)
                    throw new InvalidOperationException("Deque is empty");
                var tail = (_head + _count - 1) % _buffer.Length;
                var item = _buffer[tail];
                _buffer[tail] = default!;
                _count--;
                return item;
            }

            public T this[int index]
            {
                get
                {
                    if (index < 0 || index >= _count)
                        throw new ArgumentOutOfRangeException(nameof(index));
                    return _buffer[(_head + index) % _buffer.Length];
                }
                set
                {
                    if (index < 0 || index >= _count)
                        throw new ArgumentOutOfRangeException(nameof(index));
                    _buffer[(_head + index) % _buffer.Length] = value;
                }
            }

            public void Clear()
            {
                Array.Clear(_buffer, 0, _buffer.Length);
                _head = 0;
                _count = 0;
            }

            private void EnsureCapacity()
            {
                if (_count == _buffer.Length)
                {
                    var newCapacity = _buffer.Length * 2;
                    var newBuffer = new T[newCapacity];
                    for (var i = 0; i < _count; i++)
                    {
                        newBuffer[i] = _buffer[(_head + i) % _buffer.Length];
                    }
                    _buffer = newBuffer;
                    _head = 0;
                }
            }
        }

        private List<double> _list = null!;
        private Deque<double> _deque = null!;

        [Params(20, 100, 500)]
        public int ElementCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _list = new List<double>(ElementCount);
            _deque = new Deque<double>(ElementCount);
        }

        /// <summary>
        /// List.Insert(0, item) - O(n) per operation.
        /// This is what VirtualizingStackPanel currently does when scrolling up.
        /// </summary>
        [Benchmark(Baseline = true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void List_InsertAtBeginning()
        {
            _list.Clear();

            // Simulate scroll-up: adding items at the beginning
            for (var i = 0; i < ElementCount; i++)
            {
                _list.Insert(0, i * 1.5); // O(n) operation
            }
        }

        /// <summary>
        /// Deque.PushFront - O(1) amortized per operation.
        /// Proposed optimization for virtualization.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Deque_InsertAtBeginning()
        {
            _deque.Clear();

            // Simulate scroll-up: adding items at the beginning
            for (var i = 0; i < ElementCount; i++)
            {
                _deque.PushFront(i * 1.5); // O(1) amortized
            }
        }

        /// <summary>
        /// List.Add - O(1) amortized (baseline for scroll-down).
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void List_InsertAtEnd()
        {
            _list.Clear();

            // Simulate scroll-down: adding items at the end
            for (var i = 0; i < ElementCount; i++)
            {
                _list.Add(i * 1.5); // O(1) amortized
            }
        }

        /// <summary>
        /// Deque.PushBack - O(1) amortized.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Deque_InsertAtEnd()
        {
            _deque.Clear();

            // Simulate scroll-down: adding items at the end
            for (var i = 0; i < ElementCount; i++)
            {
                _deque.PushBack(i * 1.5); // O(1) amortized
            }
        }

        /// <summary>
        /// Simulates bidirectional scrolling with List.
        /// Mix of insertions at both ends.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void List_BidirectionalScroll()
        {
            _list.Clear();

            // Initial items
            for (var i = 0; i < ElementCount / 2; i++)
            {
                _list.Add(i * 1.5);
            }

            // Simulate scroll pattern: up, down, up, down...
            for (var i = 0; i < ElementCount / 4; i++)
            {
                _list.Insert(0, -i * 1.5); // Scroll up - O(n)
                _list.Add((ElementCount + i) * 1.5); // Scroll down - O(1)
            }
        }

        /// <summary>
        /// Simulates bidirectional scrolling with Deque.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Deque_BidirectionalScroll()
        {
            _deque.Clear();

            // Initial items
            for (var i = 0; i < ElementCount / 2; i++)
            {
                _deque.PushBack(i * 1.5);
            }

            // Simulate scroll pattern: up, down, up, down...
            for (var i = 0; i < ElementCount / 4; i++)
            {
                _deque.PushFront(-i * 1.5); // Scroll up - O(1)
                _deque.PushBack((ElementCount + i) * 1.5); // Scroll down - O(1)
            }
        }

        /// <summary>
        /// Random access pattern with List.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public double List_RandomAccess()
        {
            _list.Clear();
            for (var i = 0; i < ElementCount; i++)
            {
                _list.Add(i * 1.5);
            }

            double sum = 0;
            var random = new Random(42);
            for (var i = 0; i < ElementCount; i++)
            {
                sum += _list[random.Next(ElementCount)];
            }
            return sum;
        }

        /// <summary>
        /// Random access pattern with Deque.
        /// </summary>
        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public double Deque_RandomAccess()
        {
            _deque.Clear();
            for (var i = 0; i < ElementCount; i++)
            {
                _deque.PushBack(i * 1.5);
            }

            double sum = 0;
            var random = new Random(42);
            for (var i = 0; i < ElementCount; i++)
            {
                sum += _deque[random.Next(ElementCount)];
            }
            return sum;
        }
    }
}
