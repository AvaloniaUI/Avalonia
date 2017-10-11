using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.DesignerSupport;
using Avalonia.Input;
using Avalonia.Remote.Protocol;
using Avalonia.Remote.Protocol.Designer;
using Avalonia.Remote.Protocol.Viewport;
using Avalonia.Threading;

namespace Avalonia.DesignerSupport.Remote
{
    public class RemoteDesignerEntryPoint
    {
        private static ClientSupportedPixelFormatsMessage s_supportedPixelFormats;
        private static ClientViewportAllocatedMessage s_viewportAllocatedMessage;
        private static IAvaloniaRemoteTransportConnection s_transport;
        class CommandLineArgs
        {
            public string AppPath { get; set; }
            public Uri Transport { get; set; }
        }

        static Exception Die(string error)
        {
            if (error != null)
            {
                Console.Error.WriteLine(error);
                Console.Error.Flush();
            }
            Environment.Exit(1);
            return new Exception("APPEXIT");
        }

        static Exception PrintUsage()
        {
            Console.Error.WriteLine("Usage: --transport transport_spec app");
            Console.Error.WriteLine();
            Console.Error.WriteLine("Example: --transport tcp-bson://127.0.0.1:30243/ MyApp.exe");
            Console.Error.Flush();
            return Die(null);
        }
        
        static CommandLineArgs ParseCommandLineArgs(string[] args)
        {
            var rv = new CommandLineArgs();
            Action<string> next = null;
            try
            {
                foreach (var arg in args)
                {
                    if (next != null)
                    {
                        next(arg);
                        next = null;
                    }
                    else if (arg == "--transport")
                        next = a => rv.Transport = new Uri(a, UriKind.Absolute);
                    else if (rv.AppPath == null)
                        rv.AppPath = arg;
                    else
                        PrintUsage();

                }
                if (rv.AppPath == null || rv.Transport == null)
                    PrintUsage();
            }
            catch
            {
                PrintUsage();
            }
            return rv;
        }

        static IAvaloniaRemoteTransportConnection CreateTransport(Uri transport)
        {
            if (transport.Scheme == "tcp-bson")
            {
                return new BsonTcpTransport().Connect(IPAddress.Parse(transport.Host), transport.Port).Result;
            }
            PrintUsage();
            return null;
        }

        interface IAppInitializer
        {
            Application GetConfiguredApp(IAvaloniaRemoteTransportConnection transport, object obj);
        }
        
        class AppInitializer<T> : IAppInitializer where T : AppBuilderBase<T>, new()
        {
            public Application GetConfiguredApp(IAvaloniaRemoteTransportConnection transport, object obj)
            {
                var builder = (AppBuilderBase<T>) obj;
                builder.UseWindowingSubsystem(() => PreviewerWindowingPlatform.Initialize(transport));
                builder.SetupWithoutStarting();
                return builder.Instance;
            }
        }

        private const string BuilderMethodName = "BuildAvaloniaApp";

        class NeverClose : ICloseable
        {
            public event EventHandler Closed;
        }
        
        public static void Main(string[] cmdline)
        {
            var args = ParseCommandLineArgs(cmdline);
            var transport = CreateTransport(args.Transport);
            var asm = Assembly.LoadFile(System.IO.Path.GetFullPath(args.AppPath));
            var entryPoint = asm.EntryPoint;
            if (entryPoint == null)
                throw Die($"Assembly {args.AppPath} doesn't have an entry point");
            var builderMethod = entryPoint.DeclaringType.GetMethod(BuilderMethodName,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (builderMethod == null)
                throw Die($"{entryPoint.DeclaringType.FullName} doesn't have a method named {BuilderMethodName}");

            var appBuilder = builderMethod.Invoke(null, null);
            var initializer =(IAppInitializer)Activator.CreateInstance(typeof(AppInitializer<>).MakeGenericType(appBuilder.GetType()));
            var app = initializer.GetConfiguredApp(transport, appBuilder);
            s_transport = transport;
            transport.OnMessage += OnTransportMessage;
            transport.OnException += (t, e) => Die(e.ToString());
            app.Run(new NeverClose());
        }


        private static void RebuildPreFlight()
        {
            PreviewerWindowingPlatform.PreFlightMessages = new List<object>
            {
                s_supportedPixelFormats,
                s_viewportAllocatedMessage
            };
        }
        
        private static void OnTransportMessage(IAvaloniaRemoteTransportConnection transport, object obj) => Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (obj is ClientSupportedPixelFormatsMessage formats)
            {
                s_supportedPixelFormats = formats;
                RebuildPreFlight();
            }
            if (obj is ClientViewportAllocatedMessage viewport)
            {
                s_viewportAllocatedMessage = viewport;
                RebuildPreFlight();
            }
            if (obj is UpdateXamlMessage xaml)
            {
                try
                {
                    DesignWindowLoader.LoadDesignerWindow(xaml.Xaml, xaml.AssemblyPath);
                    s_transport.Send(new UpdateXamlResultMessage());
                }
                catch (Exception e)
                {
                    s_transport.Send(new UpdateXamlResultMessage
                    {
                        Error = e.ToString()
                    });
                }
            }
        });
    }
}