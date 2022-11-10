using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Embedding;
using Avalonia.Controls.Remote;
using Avalonia.Controls.Remote.Server;
using Avalonia.DesignerSupport.Remote.HtmlTransport;
using Avalonia.Diagnostics.ViewModels;
using Avalonia.Markup.Xaml;
using Avalonia.Remote.Protocol;
using Avalonia.Remote.Protocol.Viewport;
using Avalonia.Styling;

namespace Avalonia.Diagnostics.Views;

internal class DevToolsRemoteServer : EmbeddableControlRoot, IStyleHost, IStyledElement
{
    IStyleHost? IStyleHost.StylingParent => null;
    
    [Obsolete("Compiler-only", true)]
    public DevToolsRemoteServer() 
    {
    
    }
    
    public DevToolsRemoteServer(IAvaloniaRemoteTransportConnection transport) : base(new RemoteServerTopLevelImpl(transport))
    {
        AvaloniaXamlLoader.Load(this);
        Width = Height = 1024;
        
        if (Theme is null && this.FindResource(typeof(EmbeddableControlRoot)) is ControlTheme topLevelTheme)
            Theme = topLevelTheme;
    }

    public Type StyleKey => typeof(EmbeddableControlRoot);

    class ZeroSignal : IAvaloniaRemoteTransportConnection
    {
        public void Dispose()
        {
            
        }

        public Task Send(object data)
        {
            return Task.CompletedTask;
        }

        public void SendMessage(object message) => OnMessage?.Invoke(this, message);

        public event Action<IAvaloniaRemoteTransportConnection, object>? OnMessage;
        public event Action<IAvaloniaRemoteTransportConnection, Exception>? OnException;
        public void Start()
        {
        }
    }
    
    public static IDisposable Start(TopLevel target, Uri listenUri)
    {
        var signal = new ZeroSignal();
        var transport = new HtmlWebSocketTransport(signal, listenUri);
        var vm = new MainViewModel(target);
        var server = new DevToolsRemoteServer(transport)
        {
            DataContext = vm,
            Content = vm
        };
        server.ClientSize = new Size(1024, 1024);
        server.Prepare();
        server.Renderer.Start();
        transport.Start();
        signal.SendMessage(new ClientViewportAllocatedMessage()
        {
            Height = 1024,
            Width = 1024,
            DpiX = 96,
            DpiY = 96
        });
        
        DevTools.Open(server);
        return Disposable.Create(() =>
        {
            server.Renderer.Stop();
            server.Dispose();
            transport.Dispose();
        });
        
        
    }
}