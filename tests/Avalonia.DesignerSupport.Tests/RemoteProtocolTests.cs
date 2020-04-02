using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Remote.Protocol;
using Avalonia.Remote.Protocol.Designer;
using Avalonia.Remote.Protocol.Viewport;
using Xunit;

namespace Avalonia.DesignerSupport.Tests
{
    public class RemoteProtocolTests : IDisposable
    {
        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private IAvaloniaRemoteTransportConnection _server;
        private IAvaloniaRemoteTransportConnection _client;
        private BlockingCollection<object> _serverMessages = new BlockingCollection<object>();
        private BlockingCollection<object> _clientMessages = new BlockingCollection<object>();
        private SynchronizationContext _originalContext;


        class DisabledSyncContext : SynchronizationContext
        {
            public override void Post(SendOrPostCallback d, object state)
            {
                throw new InvalidCastException("Not allowed");
            }

            public override void Send(SendOrPostCallback d, object state)
            {
                throw new InvalidCastException("Not allowed");
            }
        }
        
        void Init(IMessageTypeResolver clientResolver = null, IMessageTypeResolver serverResolver = null)
        {
            _originalContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(new DisabledSyncContext());
            var clientTransport = new BsonTcpTransport(clientResolver ?? new DefaultMessageTypeResolver());
            var serverTransport = new BsonTcpTransport(serverResolver ?? new DefaultMessageTypeResolver());

            var tcpListener = new TcpListener(IPAddress.Loopback, 0);
            tcpListener.Start();
            var port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
            tcpListener.Stop();

            var tcs = new TaskCompletionSource<int>();
            serverTransport.Listen(IPAddress.Loopback, port, connected =>
            {
                _server = connected;
                tcs.SetResult(0);
            });
            _client = clientTransport.Connect(IPAddress.Loopback, port).Result;
            _disposables.Add(_client);
            _client.OnMessage += (_, m) => _clientMessages.Add(m);
            tcs.Task.Wait();
            _disposables.Add(_server);
            _server.OnMessage += (_, m) => _serverMessages.Add(m);

        }

        object TakeServer()
        {
            var src = new CancellationTokenSource(200);
            try
            {
                return _serverMessages.Take(src.Token);
            }
            finally
            {
                src.Dispose();
            }

        }
        
        [Fact]
        void EntitiesAreProperlySerializedAndDeserialized()
        {
            Init();
            var rnd = new Random();
            _server.OnMessage += (_, message) => { };


            object GetRandomValue(Type t, string pathInfo)
            {
                if (t.IsArray)
                {
                    var arr = Array.CreateInstance(t.GetElementType(), 1);
                    ((IList)arr)[0] = GetRandomValue(t.GetElementType(), pathInfo);
                    return arr;
                }

                if (t == typeof(bool))
                    return true;
                if (t == typeof(int) || t == typeof(long))
                    return rnd.Next();
                if (t == typeof(byte))
                    return (byte)rnd.Next(255);
                if (t == typeof(double))
                    return rnd.NextDouble();
                if (t.IsEnum)
                    return ((IList)Enum.GetValues(t)).Cast<object>().Last();
                if (t == typeof(string))
                    return Guid.NewGuid().ToString();
                if (t == typeof(Guid))
                    return Guid.NewGuid();
                if (t == typeof(Exception))
                    return new Exception("Here");
                if (t == typeof(ExceptionDetails))
                    return new ExceptionDetails
                    {
                        ExceptionType = "Exception",
                        LineNumber = 5,
                        LinePosition = 6,
                        Message = "Here",
                    };
                throw new Exception($"Doesn't know how to fabricate a random value for {t}, path {pathInfo}");
            }
            
            foreach (var t in typeof(MeasureViewportMessage).Assembly.GetTypes().Where(t =>
                t.GetCustomAttribute(typeof(AvaloniaRemoteMessageGuidAttribute)) != null))
            {
                var o = Activator.CreateInstance(t);
                foreach (var p in t.GetProperties())
                    p.SetValue(o, GetRandomValue(p.PropertyType, $"{t.FullName}.{p.Name}"));

                _client.Send(o).Wait(200);
                var received = TakeServer();
                Helpers.StructDiff(received, o);

            }


        }

        [Fact]
        void RemoteProtocolShouldBeBackwardsCompatible()
        {
            Init(new DefaultMessageTypeResolver(typeof(ExtendedMeasureViewportMessage).Assembly));
            _client.Send(new ExtendedMeasureViewportMessage()
            {
                Width = 100, Height = 200, SomeNewProperty = 300,
                SomeArrayProperty = new[]{1,2,3},
                SubObjectProperty = new ExtendedMeasureViewportMessage.SubObject()
                {
                    Foo = 543
                }
            });
            var received = (MeasureViewportMessage)TakeServer();
            Assert.Equal(100, received.Width);
            Assert.Equal(200, received.Height);

        }

        public void Dispose()
        {
            _disposables.ForEach(d => d.Dispose());
            SynchronizationContext.SetSynchronizationContext(_originalContext);
        }
    }
    
    [AvaloniaRemoteMessageGuid("6E3C5310-E2B1-4C3D-8688-01183AA48C5B")]
    public class ExtendedMeasureViewportMessage
    {
        public double Width { get; set; }
        
        public int SomeNewProperty { get; set; }
        public int[] SomeArrayProperty { get; set; }
        public class SubObject
        {
            public int Foo { get; set; }
        }
        public SubObject SubObjectProperty { get; set; }
        public double Height { get; set; }
    }
}
