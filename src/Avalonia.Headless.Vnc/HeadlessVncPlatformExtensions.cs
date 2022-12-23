using System.Net;
using System.Net.Sockets;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless;
using Avalonia.Headless.Vnc;
using RemoteViewing.Vnc;
using RemoteViewing.Vnc.Server;

namespace Avalonia
{
    public static class HeadlessVncPlatformExtensions
    {
        public static int StartWithHeadlessVncPlatform(
            this AppBuilder builder,
            string host, int port,
            string[] args, ShutdownMode shutdownMode = ShutdownMode.OnLastWindowClose)
        {
            var tcpServer = new TcpListener(host == null ? IPAddress.Loopback : IPAddress.Parse(host), port);
            tcpServer.Start();    
            return builder
                .UseHeadless(new AvaloniaHeadlessPlatformOptions
                {
                    UseCompositor = true,
                    UseHeadlessDrawing = false
                })
                .AfterSetup(_ =>
                {
                    var lt = ((IClassicDesktopStyleApplicationLifetime)builder.Instance.ApplicationLifetime);
                    lt.Startup += async delegate
                    {
                        while (true)
                        {
                            var client = await tcpServer.AcceptTcpClientAsync();
                            var options = new VncServerSessionOptions
                            {
                                AuthenticationMethod = AuthenticationMethod.None
                            };
                            var session = new VncServerSession();
                            
                            session.SetFramebufferSource(new HeadlessVncFramebufferSource(
                                session, lt.MainWindow));
                            session.Connect(client.GetStream(), options);
                        }
                        
                    };
                })
                .StartWithClassicDesktopLifetime(args, shutdownMode);
        }
    }
}
