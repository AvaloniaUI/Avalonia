using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Remote.Protocol
{
    public interface IAvaloniaRemoteTransport
    {
        Task Send(object data);
        event Action<object> OnMessage;
        event Action<Exception> OnException;
    }
}
