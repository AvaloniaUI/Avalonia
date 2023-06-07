using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;

namespace Avalonia.Remote.Protocol
{
    [RequiresUnreferencedCode("Bson uses reflection")]
    public class BsonTcpTransport : TcpTransportBase
    {
        public BsonTcpTransport(IMessageTypeResolver resolver) : base(resolver)
        {
        }

        public BsonTcpTransport() : this(new DefaultMessageTypeResolver(typeof(BsonTcpTransport).GetTypeInfo().Assembly))
        {
            
        }

        protected override IAvaloniaRemoteTransportConnection CreateTransport(IMessageTypeResolver resolver,
            Stream stream, Action dispose)
        {
            var t = new BsonStreamTransportConnection(resolver, stream, stream, dispose);
            var wrap = new TransportConnectionWrapper(t);
            t.StartReading();
            return wrap;
        }
    }
}
