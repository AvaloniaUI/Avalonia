using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Diagnostics.Views;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Avalonia.Diagnostics
{
    public static class DevTools
    {
        private static readonly Dictionary<TopLevel, Window> s_open = new Dictionary<TopLevel, Window>();

        public static IDisposable Attach(TopLevel root, KeyGesture gesture)
        {
            void PreviewKeyDown(object sender, KeyEventArgs e)
            {
                if (gesture.Matches(e))
                {
                    Open(root);
                }
            }

            return root.AddHandler(
                InputElement.KeyDownEvent,
                PreviewKeyDown,
                RoutingStrategies.Tunnel);
        }

        public static IDisposable Open(TopLevel root)
        {
            if (s_open.TryGetValue(root, out var window))
            {
                window.Activate();
            }
            else
            {
                window = new MainWindow
                {
                    Width = 1024,
                    Height = 512,
                    Root = root,
                };

                window.Closed += DevToolsClosed;
                s_open.Add(root, window);
                window.Show();
            }

            return Disposable.Create(() => window?.Close());
        }

        private static void DevToolsClosed(object sender, EventArgs e)
        {
            var window = (MainWindow)sender;
            s_open.Remove(window.Root);
            window.Closed -= DevToolsClosed;
        }
    }
}
