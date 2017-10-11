using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls.Embedding;
using Avalonia.Controls.Remote.Server;
using Avalonia.Platform;
using Avalonia.Remote.Protocol;

namespace Avalonia.Controls.Remote
{
    public class RemoteServer
    {
        private EmbeddableControlRoot _topLevel;

        class EmbeddableRemoteServerTopLevelImpl : RemoteServerTopLevelImpl, IEmbeddableWindowImpl
        {
            public EmbeddableRemoteServerTopLevelImpl(IAvaloniaRemoteTransportConnection transport) : base(transport)
            {
            }
#pragma warning disable 67
            public event Action LostFocus;

        }
        
        public RemoteServer(IAvaloniaRemoteTransportConnection transport)
        {
            _topLevel = new EmbeddableControlRoot(new EmbeddableRemoteServerTopLevelImpl(transport));
            _topLevel.Prepare();
            //TODO: Somehow react on closed connection?
        }

        public object Content
        {
            get => _topLevel.Content;
            set => _topLevel.Content = value;
        }
    }
}
