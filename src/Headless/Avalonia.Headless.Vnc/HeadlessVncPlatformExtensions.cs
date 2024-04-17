using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless;
using Avalonia.Headless.Vnc;
using Avalonia.Platform;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using RemoteViewing.Vnc;
using RemoteViewing.Vnc.Server;

namespace Avalonia
{
    public static class HeadlessVncPlatformExtensions
    {
        public static int StartWithHeadlessVncPlatform(
            this AppBuilder builder,
            string host, int port,
            string[] args,
            ShutdownMode shutdownMode = ShutdownMode.OnLastWindowClose,
            string? password = null,
            ILogger? logger = null)
        {
            logger = logger ??= NullLogger.Instance;
            var tcpServer = new TcpListener(host == null ? IPAddress.Loopback : IPAddress.Parse(host), port);
            tcpServer.Start();
            return builder
                .UseHeadless(new AvaloniaHeadlessPlatformOptions
                {
                    UseHeadlessDrawing = false,
                    FrameBufferFormat = PixelFormat.Bgra8888
                })
                .AfterApplicationSetup(_ =>
                {
                    var lt = ((IClassicDesktopStyleApplicationLifetime) builder.Instance!.ApplicationLifetime!);
                    lt.Startup += async delegate
                    {
                        while (true)
                        {
                            try
                            {
                                var client = await tcpServer.AcceptTcpClientAsync();
                                var options = new VncServerSessionOptions
                                {
                                    AuthenticationMethod = string.IsNullOrWhiteSpace(password)
                                        ? AuthenticationMethod.None
                                        : AuthenticationMethod.Password
                                };
                                var session = new VncServerSession(new VncPasswordChallenge(),
                                    logger: logger);
                                if (string.IsNullOrWhiteSpace(password) == false)
                                {
                                    session.PasswordProvided += (s, e) =>
                                    {
                                        e.Accept(password.ToCharArray());
                                    };
                                }

                                session.SetFramebufferSource(new HeadlessVncFramebufferSource(
                                    session,
                                    lt.MainWindow ??
                                    throw new InvalidOperationException("MainWindow wasn't initialized")));
                                session.Connect(client.GetStream(), options);
                            }
                            catch (Exception e)
                            {
                                logger.LogError(e,"VNC Connection Exception");
                            }
                            finally
                            {
                                await Task.Delay(100);
                            }
                        }
                    };
                })
                .StartWithClassicDesktopLifetime(args, shutdownMode);
        }
    }
}
