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
        public static int StartWithHeadlessVncPlatform<T>(
            this T builder,
            string host, int port,
            string[] args, ShutdownMode shutdownMode = ShutdownMode.OnLastWindowClose)
            where T : AppBuilderBase<T>, new()
        {
            var tcpServer = new TcpListener(host == null ? IPAddress.Loopback : IPAddress.Parse(host), port);
            tcpServer.Start();    
            return builder
                .UseHeadless(false)
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
