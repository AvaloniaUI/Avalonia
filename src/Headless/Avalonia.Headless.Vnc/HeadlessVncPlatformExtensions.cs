using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless;
using Avalonia.Headless.Vnc;
using Avalonia.Logging;
using Avalonia.Platform;
using RemoteViewing.Vnc;
using RemoteViewing.Vnc.Server;

namespace Avalonia
{
    public static class HeadlessVncPlatformExtensions
    {
        /// <summary>
        /// Start Avalonia application with Headless VNC platform without password.
        /// </summary>
        /// <param name="builder">Application Builder</param>
        /// <param name="host">VNC Server IP will be bind, if null or empty IPAddress.LoopBack will be used.</param>
        /// <param name="port">VNC Server port will be bind</param>
        /// <param name="args">Avalonia application start args</param>
        /// <param name="shutdownMode">shut down mode <see cref="ShutdownMode"/></param>
        /// <returns></returns>
        public static int StartWithHeadlessVncPlatform(
            this AppBuilder builder,
            string host, int port,
            string[] args,
            ShutdownMode shutdownMode = ShutdownMode.OnLastWindowClose)
        {
            return StartWithHeadlessVncPlatform(builder, host, port, null, args, shutdownMode);
        }

        /// <summary>
        /// Start Avalonia application with Headless VNC platform with password.
        /// </summary>
        /// <param name="builder">Application Builder</param>
        /// <param name="host">VNC Server IP will be bind, if null or empty IPAddress.LoopBack will be used.</param>
        /// <param name="port">VNC Server port will be bind</param>
        /// <param name="password">VNC connection auth password</param>
        /// <param name="args">Avalonia application start args</param>
        /// <param name="shutdownMode">shut down mode <see cref="ShutdownMode"/></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static int StartWithHeadlessVncPlatform(
            this AppBuilder builder,
            string host, int port,
            string? password,
            string[] args,
            ShutdownMode shutdownMode = ShutdownMode.OnLastWindowClose)
        {
            var vncLogger = new AvaloniaVncLogger();
            var tcpServer = new TcpListener(string.IsNullOrEmpty(host) ? IPAddress.Loopback : IPAddress.Parse(host), port);
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
                                var session = new VncServerSession(new VncPasswordChallenge(), logger:vncLogger);
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
                                Logger.TryGet(LogEventLevel.Error, LogArea.VncPlatform)?.Log(tcpServer,"Error accepting client:{Exception}", e);
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
