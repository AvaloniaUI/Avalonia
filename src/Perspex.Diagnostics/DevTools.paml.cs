using System;
using Perspex.Controls;
using Perspex.Controls.Templates;
using Perspex.Diagnostics.ViewModels;
using Perspex.Input;
using Perspex.Interactivity;
using Perspex.Markup.Xaml;
using ReactiveUI;

namespace Perspex.Diagnostics
{
    public class DevTools : UserControl
    {
        public DevTools(IControl root)
        {
            this.InitializeComponent();
            this.DataContext = new DevToolsViewModel(root);
        }

        public static IDisposable Attach(Window window)
        {
            return window.AddHandler(
                KeyDownEvent,
                WindowPreviewKeyDown,
                RoutingStrategies.Tunnel);
        }

        private static void WindowPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F12)
            {
                Window window = new Window
                {
                    Width = 1024,
                    Height = 512,
                    Content = new DevTools((IControl)sender),
                    DataTemplates = new DataTemplates
                    {
                        new ViewLocator<ReactiveObject>(),
                    }
                };

                window.Show();
            }
        }

        private void InitializeComponent()
        {
            PerspexXamlLoader.Load(this);
        }
    }
}
