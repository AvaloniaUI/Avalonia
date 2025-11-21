using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Remote.Protocol;
using Avalonia.Remote.Protocol.Designer;
using Avalonia.Remote.Protocol.Input;
using Avalonia.Remote.Protocol.Viewport;
using Avalonia.Threading;
using Java.Text;

namespace Avalonia.Android.Previewer
{
    internal class PreviewerConnection(PreviewPresentation previewPresentation) : IDisposable
    {
        private IDisposable? _listener;
        private IAvaloniaRemoteTransportConnection? _connection;
        private readonly string _sessionId = Guid.NewGuid().ToString();

        public void Dispose()
        {
            if (_connection != null)
            {
                _connection.OnMessage -= Transport_OnMessage;
                _connection.OnException -= Transport_OnException;
                _connection.Dispose();
            }
            _listener?.Dispose();
            _connection = null;
            _listener = null;
        }

        [RequiresUnreferencedCode("Calls Avalonia.Remote.Protocol.BsonTcpTransport.BsonTcpTransport()")]
        public async void Listen(int port)
        {
            _listener = new BsonTcpTransport().Listen(System.Net.IPAddress.Loopback, port,
            async t =>
            {
                try
                {
                    _connection = t;
                    _connection.OnMessage += Transport_OnMessage;
                    _connection.OnException += Transport_OnException;
                    await _connection.Send(new StartDesignerSessionMessage { SessionId = _sessionId });
                }
                catch (Exception ex)
                {
                }
            });
        }

        private void Transport_OnException(IAvaloniaRemoteTransportConnection arg1, Exception arg2)
        {
            Logger.TryGet(LogEventLevel.Error, LogArea.AndroidPlatform)?
                .Log(this, $"Previewer Exception: {arg2.ToString()}");
        }

        public void Send(object obj)
        {
            _connection?.Send(obj);
        }

        private void Transport_OnMessage(IAvaloniaRemoteTransportConnection transport, object arg2) => Dispatcher.UIThread.Post(async arg =>
        {
            switch (arg2)
            {

                case ClientRenderInfoMessage renderInfo:
                    previewPresentation.RenderScaling = (float)(renderInfo.DpiX / 96.0);
                    break;

                case UpdateXamlMessage xaml:
                    try
                    {
                        await previewPresentation.UpdateXaml(xaml.Xaml);
                        _connection?.Send(new UpdateXamlResultMessage() { Handle = previewPresentation.View?.TopLevel?.PlatformImpl?.Handle?.ToString() });
                    }
                    catch (Exception e)
                    {
                        _connection?.Send(new UpdateXamlResultMessage
                        {
                            Error = e.ToString(),
                            Exception = new ExceptionDetails(e),
                        });
                    }
                    break;
            }
        }, arg2);
    }
}
