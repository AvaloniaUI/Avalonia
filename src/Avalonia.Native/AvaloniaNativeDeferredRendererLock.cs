using System;
using System.Reactive.Disposables;
using Avalonia.Native.Interop;
using Avalonia.Rendering;

namespace Avalonia.Native
{
    public class AvaloniaNativeDeferredRendererLock : IDeferredRendererLock
    {
        private readonly IAvnWindowBase _window;

        public AvaloniaNativeDeferredRendererLock(IAvnWindowBase window)
        {
            _window = window;
        }
        public IDisposable TryLock()
        {
            if (_window.TryLock())
                return Disposable.Create(() => _window.Unlock());
            return null;
        }
    }
}
