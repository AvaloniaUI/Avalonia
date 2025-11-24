using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace Avalonia.Android.Previewer
{
    public class PreviewFactoryActivity<TApp> : AvaloniaActivity where TApp : Application, new()
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if (OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                var currentIntent = this.Intent;
                var port = FreeTcpPort();
                if (port != 0)
                {
                    var metrics = this.Resources?.DisplayMetrics;
                    if (metrics is { } m)
                    {
                        var display = PreviewDisplay.GetOrCreateDisplay(metrics, this);

                        display?.StartDisplay();

                        if (display != null)
                         {
                             var assembly = Assembly.GetAssembly(typeof(TApp));
                             var presentation = new PreviewPresentation(this, display.Display, port, assembly);
                             presentation.Show();
                    }
                }

                    global::Android.Util.Log.Info("AVALONIA_PREVIEW", $"Previewer started at port-{port}");
                }
            }

            Finish();
        }

        protected static int FreeTcpPort()
        {
            var l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            l.Dispose();
            return port;
        }
    }
}
