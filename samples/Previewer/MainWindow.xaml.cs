using System;
using System.Net;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Remote;
using Avalonia.Markup.Xaml;
using Avalonia.Remote.Protocol;
using Avalonia.Remote.Protocol.Designer;
using Avalonia.Remote.Protocol.Viewport;
using Avalonia.Threading;

namespace Previewer
{
    public class MainWindow : Window
    {
        private const string InitialXaml = @"<Window xmlns=""https://github.com/avaloniaui"" Width=""600"" Height=""500"">
        <TextBlock>Hello world!</TextBlock>
    
        </Window>";
        private IAvaloniaRemoteTransportConnection _connection;
        private Control _errorsContainer;
        private TextBlock _errors;
        private RemoteWidget _remote;


        public MainWindow()
        {
            this.InitializeComponent();
            var tb = this.FindControl<TextBox>("Xaml");
            tb.Text = InitialXaml;
            var scroll = this.FindControl<ScrollViewer>("Remote");
            var rem = new Center();
            scroll.Content = rem;
            _errorsContainer = this.FindControl<Control>("ErrorsContainer");
            _errors = this.FindControl<TextBlock>("Errors");
            tb.GetObservable(TextBox.TextProperty).Subscribe(text => _connection?.Send(new UpdateXamlMessage
            {
                Xaml = text
            }));
            new BsonTcpTransport().Listen(IPAddress.Loopback, 25000, t =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (_connection != null)
                    {
                        _connection.Dispose();
                        _connection.OnMessage -= OnMessage;
                    }
                    _connection = t;
                    rem.Child = _remote = new RemoteWidget(t);
                    t.Send(new UpdateXamlMessage
                    {
                        Xaml = tb.Text
                    });
                    
                    t.OnMessage += OnMessage;
                });
            });
            Title = "Listening on 127.0.0.1:25000";
        }

        private void OnMessage(IAvaloniaRemoteTransportConnection transport, object obj)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (transport != _connection)
                    return;
                if (obj is UpdateXamlResultMessage result)
                {
                    _errorsContainer.IsVisible = result.Error != null;
                    _errors.Text = result.Error ?? "";
                }
                if (obj is RequestViewportResizeMessage resize)
                {
                    _remote.Width = Math.Min(4096, Math.Max(resize.Width, 1));
                    _remote.Height = Math.Min(4096, Math.Max(resize.Height, 1));
                }
            });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
