// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Avalonia.Threading
{
    /// <summary>
    /// Generic implementation of object pooling pattern.
    /// Uses a factory to create new instances.
    /// </summary>
    /// <typeparam name="T">Object type.</typeparam>
    public class ThreadSafeObjectPoolWithFactory<T> where T : class
    {
        private readonly Stack<T> _stack = new Stack<T>();
        private readonly object _lock = new object();
        private readonly Func<T> _factory;

        /// <summary>
        /// Create new object pool.
        /// </summary>
        /// <param name="factory">Factory that will be used to create new instances.</param>
        public ThreadSafeObjectPoolWithFactory(Func<T> factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <summary>
        /// Gets new object from the pool. If there is none a new instance will be allocated.
        /// </summary>
        /// <returns>New object.</returns>
        public T Get()
        {
            lock (_lock)
            {
                if (_stack.Count == 0)
                    return _factory();
                return _stack.Pop();
            }
        }

        /// <summary>
        /// Return object to the pool.
        /// </summary>
        /// <param name="obj">Object to return.</param>
        public void Return(T obj)
        {
            lock (_lock)
            {
                _stack.Push(obj);
            }
        }
    }
}
