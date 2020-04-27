// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Avalonia.Collections.Pooled
{
    internal sealed class QueueDebugView<T>
    {
        private readonly PooledQueue<T> _queue;

        public QueueDebugView(PooledQueue<T> queue)
        {
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                return _queue.ToArray();
            }
        }
    }
}
