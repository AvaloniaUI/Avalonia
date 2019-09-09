// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Threading;

namespace Avalonia.Rendering
{
    public class ManagedDeferredRendererLock : IDeferredRendererLock
    {
        private readonly object _lock = new object();

        /// <summary>
        /// Tries to lock the target surface or window
        /// </summary>
        /// <returns>IDisposable if succeeded to obtain the lock</returns>
        public IDisposable TryLock()
        {
            if (Monitor.TryEnter(_lock))
                return new UnlockDisposable(_lock);
            return null;
        }

        /// <summary>
        /// Enters a waiting lock, only use from platform code, not from the renderer
        /// </summary>
        public IDisposable Lock()
        {
            Monitor.Enter(_lock);
            return new UnlockDisposable(_lock);
        }

        private sealed class UnlockDisposable : IDisposable
        {
            private object _lock;

            public UnlockDisposable(object @lock)
            {
                _lock = @lock;
            }

            public void Dispose()
            {
                object @lock = Interlocked.Exchange(ref _lock, null);

                if (@lock != null)
                {
                    Monitor.Exit(@lock);
                }
            }
        }
    }
}
