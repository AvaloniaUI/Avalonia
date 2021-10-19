using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Diagnostics.Views;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;

namespace Avalonia.Diagnostics
{
    public static class DevTools
    {
        private static readonly Dictionary<AvaloniaObject, MainWindow> s_open =
            new Dictionary<AvaloniaObject, MainWindow>();

        private static bool s_attachedToApplication;

        public static IDisposable Attach(TopLevel root, KeyGesture gesture)
        {
            return Attach(root, new DevToolsOptions()
            {
                Gesture = gesture,
            });
        }

        public static IDisposable Attach(TopLevel root, DevToolsOptions options)
        {
            if (s_attachedToApplication == true)
            {
                throw new ArgumentException("DevTools already attached to application", nameof(root));
            }

            void PreviewKeyDown(object? sender, KeyEventArgs e)
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

        public static IDisposable Open(TopLevel root) => Open((AvaloniaObject)root);

        private static IDisposable Open(AvaloniaObject root) => Open(root, new DevToolsOptions());

        public static IDisposable Open(TopLevel root, DevToolsOptions options) => Open((AvaloniaObject)root, options);

        private static IDisposable Open(AvaloniaObject root, DevToolsOptions options)
        {
            if (s_open.TryGetValue(root, out var window))
            {
                window.Activate();
            }
            else
            {
                window = new MainWindow
                {
                    Root = root,
                    Width = options.Size.Width,
                    Height = options.Size.Height,
                };
                window.SetOptions(options);

                window.Closed += DevToolsClosed;
                s_open.Add(root, window);

                if (options.ShowAsChildWindow && root is Window inspectedWindow)
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

        private static void DevToolsClosed(object? sender, EventArgs e)
        {
            var window = (MainWindow)sender!;
            window.Closed -= DevToolsClosed;
            if (window.Root is Controls.Application host)
            {
                s_open.Remove(host.Instance);
            }
            else
            {
                s_open.Remove(window.Root!);
            }                       
        }

        internal static void Attach(Application application, DevToolsOptions options)
        {
            if (s_attachedToApplication == true)
            {
                throw new ArgumentException("DevTools already attached to application", nameof(application));
            }
            var lifeTime = application.ApplicationLifetime
                as Avalonia.Controls.ApplicationLifetimes.IControlledApplicationLifetime;

            if (lifeTime is null)
            {
                throw new ArgumentNullException(nameof(Application.ApplicationLifetime));
            }
            s_attachedToApplication = true;

            application.InputManager.PreProcess.OfType<RawKeyEventArgs>().Subscribe(e =>
                {
                    if (options.Gesture.Matches(e))
                    {
                        Open(application, options);
                    }
                });
        }

        private static IDisposable Open(Application application, DevToolsOptions options)
        {

            if (s_open.TryGetValue(application, out var window))
            {                
                window.Activate();
            }
            else
            {
                window = new MainWindow
                {
                    Root = new Controls.Application(application),
                    Width = options.Size.Width,
                    Height = options.Size.Height,
                };
                window.SetOptions(options);

                window.Closed += DevToolsClosed;
                s_open.Add(application, window);
                window.Show();
            }
            return Disposable.Create(() => window?.Close());
        }
    }
}
