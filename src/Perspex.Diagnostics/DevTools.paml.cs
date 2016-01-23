using System;
using System.Collections.Generic;
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
        private static Dictionary<Window, Window> s_open = new Dictionary<Window, Window>();

        public DevTools(IControl root)
        {
            InitializeComponent();
            Root = root;
            DataContext = new DevToolsViewModel(root);
        }

        public IControl Root { get; }

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
                var window = (Window)sender;
                var devToolsWindow = default(Window);

                if (s_open.TryGetValue(window, out devToolsWindow))
                {
                    devToolsWindow.Activate();
                }
                else
                {
                    devToolsWindow = new Window
                    {
                        Width = 1024,
                        Height = 512,
                        Content = new DevTools(window),
                        DataTemplates = new DataTemplates
                        {
                            new ViewLocator<ReactiveObject>(),
                        }
                    };

                    devToolsWindow.Closed += DevToolsClosed;
                    s_open.Add((Window)sender, devToolsWindow);
                    devToolsWindow.Show();
                }
            }
        }

        private static void DevToolsClosed(object sender, EventArgs e)
        {
            var devToolsWindow = (Window)sender;
            var devTools = (DevTools)devToolsWindow.Content;
            var window = (Window)devTools.Root;

            s_open.Remove(window);
            devToolsWindow.Closed -= DevToolsClosed;
        }

        private void InitializeComponent()
        {
            PerspexXamlLoader.Load(this);
        }
    }
}
