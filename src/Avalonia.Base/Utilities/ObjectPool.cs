using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Avalonia.Utilities
{
    /// <summary>
    /// Provides a thread-safe object pool with size limits and object validation.
    /// </summary>
    /// <typeparam name="T">The type of objects to pool.</typeparam>
    internal sealed class ObjectPool<T> where T : class
    {
        private readonly Func<T> _factory;
        private readonly Func<T, bool>? _validator;
        private readonly ConcurrentBag<T> _items;
        private readonly int _maxSize;
        private int _count;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectPool{T}"/> class.
        /// </summary>
        /// <param name="factory">Factory function to create new instances.</param>
        /// <param name="validator">Optional validator to clean and validate objects before returning to the pool. Return false to discard the object.</param>
        /// <param name="maxSize">Maximum number of objects to keep in the pool. Default is 32.</param>
        public ObjectPool(Func<T> factory, Func<T, bool>? validator = null, int maxSize = 32)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _validator = validator;
            _maxSize = maxSize;
            _items = new ConcurrentBag<T>();
            _count = 0;
        }

        /// <summary>
        /// Rents an object from the pool or creates a new one if the pool is empty.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Rent()
        {
            if (_items.TryTake(out var item))
            {
                System.Threading.Interlocked.Decrement(ref _count);
                return item;
            }

            return _factory();
        }

        /// <summary>
        /// Returns an object to the pool if it passes validation and the pool is not full.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Return(T item)
        {
            if (item == null)
                return;

            // Validate and clean the object
            if (_validator != null && !_validator(item))
                return;

            // Check if pool is full (fast check without lock)
            if (_count >= _maxSize)
                return;

            // Try to increment count, but check again in case of race condition
            var currentCount = System.Threading.Interlocked.Increment(ref _count);
            
            if (currentCount <= _maxSize)
            {
                _items.Add(item);
            }
            else
            {
                // Pool is full, decrement and discard
                System.Threading.Interlocked.Decrement(ref _count);
            }
        }
    }
}
