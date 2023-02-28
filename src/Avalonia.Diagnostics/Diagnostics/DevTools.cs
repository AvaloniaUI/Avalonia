using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Diagnostics.Views;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.Reactive;

namespace Avalonia.Diagnostics
{
    internal static class DevTools
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
                throw new ArgumentException("DevTools already attached to application.", nameof(root));
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

        private static IDisposable Open(TopLevel root, DevToolsOptions options) =>
             Open(default, options, root);

        internal static IDisposable Attach(Application application, DevToolsOptions options)
        {
            var openedDisposable = new SerialDisposableValue();
            var result = new CompositeDisposable(2);
            result.Add(openedDisposable);

            // Skip if call on Design Mode
            if (!Design.IsDesignMode
                && !s_attachedToApplication)
            {

                var lifeTime = application.ApplicationLifetime
                    as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;

                if (lifeTime is null)
                {
                    throw new ArgumentNullException(nameof(application), "DevTools can only attach to applications that support IClassicDesktopStyleApplicationLifetime.");
                }

                var owner = TopLevel.GetTopLevel(lifeTime.MainWindow)
                    ?? throw new ArgumentException(nameof(application), "It can't retrieve TopLevel.");

                if (application.InputManager is { })
                {
                    s_attachedToApplication = true;

                    result.Add(application.InputManager.PreProcess.Subscribe(e =>
                    {
                        if (e is RawKeyEventArgs keyEventArgs
                            && keyEventArgs.Type == RawKeyEventType.KeyUp
                            && options.Gesture.Matches(keyEventArgs))
                        {
                            openedDisposable.Disposable = Open(application, options, owner);
                        }
                    }));
                }
            }
            return result;
        }

        private static IDisposable Open(Application? application, DevToolsOptions options, TopLevel owner)
        {
            var focussedControl = KeyboardDevice.Instance?.FocusedElement as Control;
            AvaloniaObject root = owner;
            AvaloniaObject key = owner;
            if (application is not null)
            {
                root = new Controls.Application(application);
                key = application;
            }

            if (s_open.TryGetValue(key, out var window))
            {
                window.Activate();
                window.SelectedControl(focussedControl);
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
                window.SelectedControl(focussedControl);
                window.Closed += DevToolsClosed;
                s_open.Add(key, window);
                if (options.ShowAsChildWindow && owner is Window ow)
                {
                    window.Show(ow);
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
    }
}
