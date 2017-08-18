using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Remote.Protocol;

namespace Avalonia.Controls.Remote
{
    public class RemoteServer
    {
        private readonly IAvaloniaRemoteTransport _transport;

        public RemoteServer(IAvaloniaRemoteTransport transport)
        {
            _transport = transport;
        }

        public object Content { get; set; }
    }
}
