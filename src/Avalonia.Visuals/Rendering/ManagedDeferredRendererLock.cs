using System;
using System.Threading;

namespace Avalonia.Rendering
{
    public class ManagedDeferredRendererLock : IDeferredRendererLock
    {
        private readonly object _lock = new object();
        private readonly LockDisposable _lockDisposable;

        public ManagedDeferredRendererLock()
        {
            _lockDisposable = new LockDisposable(_lock);
        }

        /// <summary>
        /// Tries to lock the target surface or window
        /// </summary>
        /// <returns>IDisposable if succeeded to obtain the lock</returns>
        public IDisposable TryLock()
        {
            if (Monitor.TryEnter(_lock))
                return _lockDisposable;
            return null;
        }

        /// <summary>
        /// Enters a waiting lock, only use from platform code, not from the renderer
        /// </summary>
        public IDisposable Lock()
        {
            Monitor.Enter(_lock);
            return _lockDisposable;
        }

        private class LockDisposable : IDisposable
        {
            private readonly object _lock;

            public LockDisposable(object @lock)
            {
                _lock = @lock;
            }

            public void Dispose()
            {
                Monitor.Exit(_lock);
            }
        }
    }
}
