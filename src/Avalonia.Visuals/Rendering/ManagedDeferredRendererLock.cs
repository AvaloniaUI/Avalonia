using System;
using System.Reactive.Disposables;
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
                return Disposable.Create(() => Monitor.Exit(_lock));
            return null;
        }

        /// <summary>
        /// Enters a waiting lock, only use from platform code, not from the renderer
        /// </summary>
        public IDisposable Lock()
        {
            Monitor.Enter(_lock);
            return Disposable.Create(() => Monitor.Exit(_lock));
        }
    }
}
