using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Avalonia.Remote.Protocol
{
    public class TransportConnectionWrapper : IAvaloniaRemoteTransportConnection
    {
        private readonly IAvaloniaRemoteTransportConnection _conn;
        private EventStash<object> _onMessage;
        private EventStash<Exception> _onException;
        
        private Queue<SendOperation> _sendQueue = new Queue<SendOperation>();
        private object _lock =new object();
        private TaskCompletionSource<int> _signal;
        private bool _workerIsAlive;
        public TransportConnectionWrapper(IAvaloniaRemoteTransportConnection conn)
        {
            _conn = conn;
            _onException = new EventStash<Exception>(this);
            _onMessage = new EventStash<object>(this, e => _onException.Fire(this, e));
            _conn.OnException +=_onException.Fire;
            conn.OnMessage +=  _onMessage.Fire;

        }

        class SendOperation
        {
            public object Message { get; set; }
            public TaskCompletionSource<int> Tcs { get; set; }
        }
        
        public void Dispose() => _conn.Dispose();

        async void Worker()
        {
            while (true)
            {
                SendOperation wi = null;
                lock (_lock)
                {
                    if (_sendQueue.Count != 0)
                        wi = _sendQueue.Dequeue();
                }
                if (wi == null)
                {
                    var signal = new TaskCompletionSource<int>();
                    lock (_lock)
                        _signal = signal;
                    await signal.Task.ConfigureAwait(false);
                    continue;
                }
                try
                {
                    await _conn.Send(wi.Message).ConfigureAwait(false);
                    wi.Tcs.TrySetResult(0);
                }
                catch (Exception e)
                {
                    wi.Tcs.TrySetException(e);
                }
            }    
        }
        
        public Task Send(object data)
        {
            var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_lock)
            {
                if (!_workerIsAlive)
                {
                    _workerIsAlive = true;
                    Worker();
                }
                _sendQueue.Enqueue(new SendOperation
                {
                    Message = data,
                    Tcs = tcs
                });
                if (_signal != null)
                {
                    var signal = _signal;
                    _signal = null;
                    signal.SetResult(0);
                }
            }
            return tcs.Task;
        }
        
        public event Action<IAvaloniaRemoteTransportConnection, object> OnMessage
        {
            add => _onMessage.Add(value);
            remove => _onMessage.Remove(value);
        }

        public event Action<IAvaloniaRemoteTransportConnection, Exception> OnException
        {
            add => _onException.Add(value);
            remove => _onException.Remove(value);
        }

        public void Start() => _conn.Start();
    }
}
