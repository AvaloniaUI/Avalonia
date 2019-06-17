using System;
using Avalonia.Controls.Embedding;
using Avalonia.Controls.Remote.Server;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Remote.Protocol;
using Avalonia.Threading;

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
            public Action GotFocus { get; set; }

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
