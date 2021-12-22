// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Avalonia.Collections.Pooled
{
    internal sealed class StackDebugView<T>
    {
        private readonly PooledStack<T> _stack;

        public StackDebugView(PooledStack<T> stack)
        {
            _stack = stack ?? throw new ArgumentNullException(nameof(stack));
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                return _stack.ToArray();
            }
        }
    }
}
