using System;
using System.Collections.Generic;

namespace Avalonia.Remote.Protocol
{
    class EventStash<T>
    {
        private readonly IAvaloniaRemoteTransportConnection _transport;
        private readonly Action<Exception> _exceptionHandler;
        private List<T> _stash;
        private Action<IAvaloniaRemoteTransportConnection, T> _delegate;

        public EventStash(IAvaloniaRemoteTransportConnection transport, Action<Exception> exceptionHandler = null)
        {
            _transport = transport;
            _exceptionHandler = exceptionHandler;
        }
        
        public void Add(Action<IAvaloniaRemoteTransportConnection, T> handler)
        {
            List<T> stash;
            lock (this)
            {
                var needsReplay = _delegate == null;
                _delegate += handler;
                if(!needsReplay)
                    return;

                lock (this)
                {
                    stash = _stash;
                    if(_stash == null)
                        return;
                    _stash = null;
                }
            }
            foreach (var m in stash)
            {
                if (_exceptionHandler != null)
                    try
                    {
                        _delegate?.Invoke(_transport, m);
                    }
                    catch (Exception e)
                    {
                        _exceptionHandler(e);
                    }
                else
                    _delegate?.Invoke(_transport, m);
            }
        }
        
        
        public void Remove(Action<IAvaloniaRemoteTransportConnection, T> handler)
        {
            lock (this)
                _delegate -= handler;
        }

        public void Fire(IAvaloniaRemoteTransportConnection transport, T ev)
        {
            if (_delegate == null)
            {
                lock (this)
                {
                    _stash = _stash ?? new List<T>();
                    _stash.Add(ev);
                }
            }
            else
                _delegate?.Invoke(_transport, ev);
        }
    }
}