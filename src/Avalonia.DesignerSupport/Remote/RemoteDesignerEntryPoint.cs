using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Xml;
using Avalonia.Controls;
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
        private static ClientRenderInfoMessage s_renderInfoMessage;

        private static IAvaloniaRemoteTransportConnection s_transport;
        class CommandLineArgs
        {
            public string AppPath { get; set; }
            public Uri Transport { get; set; }
            public string Method { get; set; } = Methods.AvaloniaRemote;
            public string SessionId { get; set; } = Guid.NewGuid().ToString();
        }

        static class Methods
        {
            public const string AvaloniaRemote = "avalonia-remote";
            public const string Win32 = "win32";

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

        static void Log(string message) => Console.WriteLine(message);

        static Exception PrintUsage()
        {
            Console.Error.WriteLine("Usage: --transport transport_spec --session-id sid --method method app");
            Console.Error.WriteLine();
            Console.Error.WriteLine("Example: --transport tcp-bson://127.0.0.1:30243/ --session-id 123 --method avalonia-remote MyApp.exe");
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
                    else if (arg == "--method")
                        next = a => rv.Method = a;
                    else if (arg == "--session-id")
                        next = a => rv.SessionId = a;
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
            Application GetConfiguredApp(IAvaloniaRemoteTransportConnection transport, CommandLineArgs args, object obj);
        }
        
        class AppInitializer<T> : IAppInitializer where T : AppBuilderBase<T>, new()
        {
            public Application GetConfiguredApp(IAvaloniaRemoteTransportConnection transport,
                CommandLineArgs args, object obj)
            {
                var builder = (AppBuilderBase<T>) obj;
                if (args.Method == Methods.AvaloniaRemote)
                    builder.UseWindowingSubsystem(() => PreviewerWindowingPlatform.Initialize(transport));
                if (args.Method == Methods.Win32)
                    builder.UseWindowingSubsystem("Avalonia.Win32");
                builder.SetupWithoutStarting();
                return builder.Instance;
            }
        }

        private const string BuilderMethodName = "BuildAvaloniaApp";
        
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
            Design.IsDesignMode = true;
            Log($"Obtaining AppBuilder instance from {builderMethod.DeclaringType.FullName}.{builderMethod.Name}");
            var appBuilder = builderMethod.Invoke(null, null);
            Log($"Initializing application in design mode");
            var initializer =(IAppInitializer)Activator.CreateInstance(typeof(AppInitializer<>).MakeGenericType(appBuilder.GetType()));
            var app = initializer.GetConfiguredApp(transport, args, appBuilder);
            s_transport = transport;
            transport.OnMessage += OnTransportMessage;
            transport.OnException += (t, e) => Die(e.ToString());
            Log("Sending StartDesignerSessionMessage");
            transport.Send(new StartDesignerSessionMessage {SessionId = args.SessionId});
            Dispatcher.UIThread.MainLoop(CancellationToken.None);
        }


        private static void RebuildPreFlight()
        {
            PreviewerWindowingPlatform.PreFlightMessages = new List<object>
            {
                s_supportedPixelFormats,
                s_viewportAllocatedMessage,
                s_renderInfoMessage
            };
        }

        private static Window s_currentWindow;
        private static void OnTransportMessage(IAvaloniaRemoteTransportConnection transport, object obj) => Dispatcher.UIThread.Post(() =>
        {
            if (obj is ClientSupportedPixelFormatsMessage formats)
            {
                s_supportedPixelFormats = formats;
                RebuildPreFlight();
            }
            if (obj is ClientRenderInfoMessage renderInfo)
            {
                s_renderInfoMessage = renderInfo;
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
                    s_currentWindow?.Close();
                }
                catch
                {
                    //Ignore
                }
                s_currentWindow = null;
                try
                {
                    s_currentWindow = DesignWindowLoader.LoadDesignerWindow(xaml.Xaml, xaml.AssemblyPath, xaml.XamlFileProjectPath);
                    s_transport.Send(new UpdateXamlResultMessage(){Handle = s_currentWindow.PlatformImpl?.Handle?.Handle.ToString()});
                }
                catch (Exception e)
                {
                    var xmlException = e as XmlException;
                    
                    s_transport.Send(new UpdateXamlResultMessage
                    {
                        Error = e.ToString(),
                        Exception = new ExceptionDetails
                        {
                            ExceptionType = e.GetType().FullName,
                            Message = e.Message.ToString(),
                            LineNumber = xmlException?.LineNumber,
                            LinePosition = xmlException?.LinePosition,
                        }
                    });
                }
            }
        });
    }
}
