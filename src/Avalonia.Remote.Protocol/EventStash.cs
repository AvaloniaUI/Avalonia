using System;
using System.Collections.Generic;

namespace Avalonia.Remote.Protocol
{
    public class EventStash<T>
    {
        private readonly Action<Exception> _exceptionHandler;
        private List<T> _stash;
        private Action<T> _delegate;

        public EventStash(Action<Exception> exceptionHandler = null)
        {
            _exceptionHandler = exceptionHandler;
        }
        
        public void Add(Action<T> handler)
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
                        _delegate?.Invoke(m);
                    }
                    catch (Exception e)
                    {
                        _exceptionHandler(e);
                    }
                else
                    _delegate?.Invoke(m);
            }
        }
        
        
        public void Remove(Action<T> handler)
        {
            lock (this)
                _delegate -= handler;
        }

        public void Fire(T ev)
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
                _delegate?.Invoke(ev);
        }
    }
}