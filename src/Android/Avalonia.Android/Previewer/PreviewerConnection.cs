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
        private IAvaloniaRemoteTransportConnection? _transport;
        private readonly string _sessionId = Guid.NewGuid().ToString();

        public void Dispose()
        {
            if (_transport != null)
            {
                _transport.OnMessage -= Transport_OnMessage;
                _transport.OnException -= Transport_OnException;
                _transport.Dispose();
            }
            _transport = null;
        }

        [RequiresUnreferencedCode("Calls Avalonia.Remote.Protocol.BsonTcpTransport.BsonTcpTransport()")]
        public async void Listen(int port)
        {
            _transport = await new BsonTcpTransport().Connect(System.Net.IPAddress.Loopback, port);

            _transport.OnMessage += Transport_OnMessage;
            _transport.OnException += Transport_OnException;

            _transport.Start();
            await _transport.Send(new StartDesignerSessionMessage { SessionId = _sessionId });
        }

        private void Transport_OnException(IAvaloniaRemoteTransportConnection arg1, Exception arg2)
        {
            Logger.TryGet(LogEventLevel.Error, LogArea.AndroidPlatform)?
                .Log(this, $"Previewer Exception: {arg2.ToString()}");
        }

        public void Send(object obj)
        {
            _transport?.Send(obj);
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
                        _transport?.Send(new UpdateXamlResultMessage() { Handle = previewPresentation.View?.TopLevel?.PlatformImpl?.Handle?.ToString() });
                    }
                    catch (Exception e)
                    {
                        _transport?.Send(new UpdateXamlResultMessage
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
