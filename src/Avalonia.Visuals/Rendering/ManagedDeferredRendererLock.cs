using System;
using System.Reactive.Disposables;
using System.Threading;

namespace Avalonia.Rendering
{
    public class ManagedDeferredRendererLock : IDeferredRendererLock
    {
        private readonly object _lock = new object();
        public IDisposable TryLock()
        {
            if (Monitor.TryEnter(_lock))
                return Disposable.Create(() => Monitor.Exit(_lock));
            return null;
        }
    }
}
