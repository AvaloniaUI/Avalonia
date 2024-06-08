// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Threading;

namespace Tmds.DBus
{
    class WrappedDisposable : IDisposable
    {
        private object _gate = new object();
        private bool _disposed;
        private IDisposable _disposable;
        private readonly SynchronizationContext _synchronizationContext;

        public WrappedDisposable(SynchronizationContext synchronizationContext)
        {
            _synchronizationContext = synchronizationContext;
        }

        public void Call(Action action)
        {
            if (_synchronizationContext != null && _synchronizationContext != SynchronizationContext.Current)
            {
                if (_disposed)
                {
                    return;
                }
                _synchronizationContext.Post(_ =>
                {
                    lock (_gate)
                    {
                        if (!_disposed)
                        {
                            action();
                        }
                    }
                }, null);
            }
            else
            {
                lock (_gate)
                {
                    if (!_disposed)
                    {
                        action();
                    }
                }
            }
        }

        public void Call<T>(Action<T> action, T value, bool disposes = false)
        {
            if (_synchronizationContext != null && _synchronizationContext != SynchronizationContext.Current)
            {
                if (_disposed)
                {
                    return;
                }
                _synchronizationContext.Post(_ =>
                {
                    lock (_gate)
                    {
                        if (!_disposed)
                        {
                            if (disposes)
                            {
                                Dispose();
                            }
                            action(value);
                        }
                    }
                }, null);
            }
            else
            {
                lock (_gate)
                {
                    if (!_disposed)
                    {
                        if (disposes)
                        {
                            Dispose();
                        }
                        action(value);
                    }
                }
            }
        }

        public void Dispose()
        {
            lock (_gate)
            {
                _disposed = true;
                _disposable?.Dispose();
            }
        }

        public IDisposable Disposable
        {
            set
            {
                lock (_gate)
                {
                    if (_disposable != null)
                    {
                        throw new InvalidOperationException("Already set");
                    }
                    _disposable = value;
                    if (_disposed)
                    {
                        _disposable.Dispose();
                    }
                }
            }
        }
        public bool IsDisposed
        {
            get
            {
                lock (_gate) { return _disposed; }
            }
        }
    }
}
