using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Diagnostics.Views;
using Avalonia.Input;
using Avalonia.Interactivity;
#nullable enable 
namespace Avalonia.Diagnostics
{
    internal static class DevTools
    {
        private static readonly Dictionary<TopLevel, MainWindow> s_open = new Dictionary<TopLevel, MainWindow>();

        public static IDisposable Attach(TopLevel root, KeyGesture gesture)
        {
            return Attach(root, new DevToolsOptions()
            {
                Gesture = gesture,
            });
        }

        public static IDisposable Attach(TopLevel root, DevToolsOptions options)
        {
            void PreviewKeyDown(object sender, KeyEventArgs e)
            {
                if (options.Gesture.Matches(e))
                {
                    Open(root, options);
                }
            }

            return root.AddDisposableHandler(
                InputElement.KeyDownEvent,
                PreviewKeyDown,
                RoutingStrategies.Tunnel);
        }

        private static IDisposable Open(TopLevel root, DevToolsOptions options)
        {
            if (s_open.TryGetValue(root, out var window))
            {
                window.Activate();
            }
            else
            {
                window = new MainWindow
                {
                    Width = options.Size.Width,
                    Height = options.Size.Height,
                    Root = root,
                };

                window.Closed += DevToolsClosed;
                window.ViewModel.ShouldVisualizeDirtyRects = options.ShouldVisualizeDirtyRects;
                window.ViewModel.ShouldVisualizeMarginPadding = options.ShouldVisualizeMarginPadding;
                window.ViewModel.Console.IsVisible = options.ShowConsole;
                window.ViewModel.ShowFpsOverlay = options.ShowFpsOverlay;
                window.ViewModel.ShowLayoutVisualizer = options.ShowLayoutVisualizer;                
                window.ViewModel.ShowFpsOverlay = options.ShowFpsOverlay;
                s_open.Add(root, window);

                if (options.OnTop && root is Window inspectedWindow)
                {
                    window.Show(inspectedWindow);
                }
                else
                {
                    window.Show();
                }
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
