using System;
using Avalonia.Controls.Embedding;
using Avalonia.Controls.Remote.Server;
using Avalonia.Metadata;
using Avalonia.Remote.Protocol;

namespace Avalonia.Controls.Remote
{
    [Unstable]
    public class RemoteServer : IDisposable
    {
        private EmbeddableControlRoot _topLevel;

        class EmbeddableRemoteServerTopLevelImpl : RemoteServerTopLevelImpl
        {
            public EmbeddableRemoteServerTopLevelImpl(IAvaloniaRemoteTransportConnection transport) : base(transport)
            {
            }
        }
        
        public RemoteServer(IAvaloniaRemoteTransportConnection transport)
        {
            _topLevel = new EmbeddableControlRoot(new EmbeddableRemoteServerTopLevelImpl(transport));
            _topLevel.Prepare();
            _topLevel.StartRendering();
            //TODO: Somehow react on closed connection?
        }

        public object? Content
        {
            get => _topLevel.Content;
            set => _topLevel.Content = value;
        }

        public void Dispose()
        {
            _topLevel.StopRendering();
            _topLevel.Dispose();
        }
    }
}
