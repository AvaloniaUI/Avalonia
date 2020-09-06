using System;
using System.Threading.Tasks;
using Avalonia.Remote.Protocol;

namespace Avalonia.DesignerSupport.Remote
{
    class DetachableTransportConnection : IAvaloniaRemoteTransportConnection
    {
        private IAvaloniaRemoteTransportConnection _inner;

        public DetachableTransportConnection(IAvaloniaRemoteTransportConnection inner)
        {
            _inner = inner;
            _inner.OnMessage += FireOnMessage;
        }

        public void Dispose()
        {
            if (_inner != null)
                _inner.OnMessage -= FireOnMessage;
            _inner = null;
        }

        public void FireOnMessage(IAvaloniaRemoteTransportConnection transport, object obj) => OnMessage?.Invoke(transport, obj);
        
        public Task Send(object data)
        {
            return _inner?.Send(data);
        }

        public event Action<IAvaloniaRemoteTransportConnection, object> OnMessage;

        public event Action<IAvaloniaRemoteTransportConnection, Exception> OnException
        {
            add {}
            remove {}
        }

        public void Start() => _inner?.Start();
    }
}
