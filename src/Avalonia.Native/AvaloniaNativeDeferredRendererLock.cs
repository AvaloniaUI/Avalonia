// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Threading;
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
                return new UnlockDisposable(_window);
            return null;
        }

        private sealed class UnlockDisposable : IDisposable
        {
            private IAvnWindowBase _window;

            public UnlockDisposable(IAvnWindowBase window)
            {
                _window = window;
            }

            public void Dispose()
            {
                Interlocked.Exchange(ref _window, null)?.Unlock();
            }
        }
    }
}
