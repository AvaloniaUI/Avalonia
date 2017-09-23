using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Remote.Protocol
{
    public interface IAvaloniaRemoteTransportConnection : IDisposable
    {
        Task Send(object data);
        event Action<IAvaloniaRemoteTransportConnection, object> OnMessage;
        event Action<IAvaloniaRemoteTransportConnection, Exception> OnException;
    }
}
