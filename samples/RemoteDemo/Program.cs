using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Remote;
using Avalonia.Remote.Protocol;
using Avalonia.Threading;
using ControlCatalog;

namespace RemoteDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            AppBuilder.Configure<App>().UsePlatformDetect().SetupWithoutStarting();

            var l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            var port = ((IPEndPoint) l.LocalEndpoint).Port;
            l.Stop();
            
            var transport = new BsonTcpTransport();
            transport.Listen(IPAddress.Loopback, port, sc =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    new RemoteServer(sc).Content = new MainView();
                });
            });

            var cts = new CancellationTokenSource();
            transport.Connect(IPAddress.Loopback, port).ContinueWith(t =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    var window = new Window()
                    {
                        Content = new RemoteWidget(t.Result)
                    };
                    window.Closed += delegate { cts.Cancel(); };
                    window.Show();
                });
            });
            Dispatcher.UIThread.MainLoop(cts.Token);



        }
    }
}
