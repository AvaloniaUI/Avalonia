// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using SharpGen.Runtime;

namespace Avalonia.Native
{
    public class CallbackBase : SharpGen.Runtime.IUnknown
    {
        private uint _refCount;
        private bool _disposed;
        private readonly object _lock = new object();
        private ShadowContainer _shadow;

        public CallbackBase()
        {
            _refCount = 1;
        }

        public ShadowContainer Shadow
        {
            get => _shadow;
            set
            {
                lock (_lock)
                {
                    if (_disposed && value != null)
                    {
                        throw new ObjectDisposedException("CallbackBase");
                    }

                    _shadow = value;
                }
            }
        }

        public uint AddRef()
        {
            lock (_lock)
            {
                return ++_refCount;
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (!_disposed)
                {
                    _disposed = true;
                    Release();
                }
            }
        }

        public uint Release()
        {
            lock (_lock)
            {
                _refCount--;

                if (_refCount == 0)
                {
                    Shadow?.Dispose();
                    Shadow = null;
                    Destroyed();
                }

                return _refCount;
            }
        }

        protected virtual void Destroyed()
        {

        }
    }
}
