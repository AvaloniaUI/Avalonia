using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
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
        private static readonly Dictionary<IDevToolsTopLevelGroup, MainWindow> s_open = new();

        public static IDisposable Attach(TopLevel root, KeyGesture gesture)
        {
            return Attach(root, new DevToolsOptions()
            {
                Gesture = gesture,
            });
        }

        public static IDisposable Attach(TopLevel root, DevToolsOptions options)
        {
            void PreviewKeyDown(object? sender, KeyEventArgs e)
            {
                if (options.Gesture.Matches(e))
                {
                    Open(root, options);
                }
            }

            return (root ?? throw new ArgumentNullException(nameof(root))).AddDisposableHandler(
                InputElement.KeyDownEvent,
                PreviewKeyDown,
                RoutingStrategies.Tunnel);
        }

        public static IDisposable Open(TopLevel root, DevToolsOptions options) =>
             Open(new SingleViewTopLevelGroup(root), options, root as Window, null);

        internal static IDisposable Open(IDevToolsTopLevelGroup group, DevToolsOptions options) =>
            Open(group, options, null, null);

        internal static IDisposable Attach(Application application, DevToolsOptions options)
        {
            var openedDisposable = new SerialDisposableValue();
            var result = new CompositeDisposable(2);
            result.Add(openedDisposable);

            // Skip if call on Design Mode
            if (!Design.IsDesignMode)
            {
                var lifeTime = application.ApplicationLifetime
                    as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;

                if (lifeTime is null)
                {
                    throw new ArgumentNullException(nameof(application), "DevTools can only attach to applications that support IClassicDesktopStyleApplicationLifetime.");
                }

                if (application.InputManager is not null)
                {
                    result.Add(application.InputManager.PreProcess.Subscribe(e =>
                    {
                        var owner = lifeTime.MainWindow;

                        if (e is RawKeyEventArgs keyEventArgs
                            && keyEventArgs.Type == RawKeyEventType.KeyUp
                            && options.Gesture.Matches(keyEventArgs))
                        {
                            openedDisposable.Disposable =
                                Open(new ClassicDesktopStyleApplicationLifetimeTopLevelGroup(lifeTime), options,
                                    owner, application);
                            e.Handled = true;
                        }
                    }));
                }
            }
            return result;
        }

        private static IDisposable Open(IDevToolsTopLevelGroup topLevelGroup, DevToolsOptions options,
            Window? owner, Application? app)
        {
            var focusedControl = owner?.FocusManager?.GetFocusedElement() as Control;
            AvaloniaObject root = topLevelGroup switch
            {
                ClassicDesktopStyleApplicationLifetimeTopLevelGroup gr => new Controls.Application(gr, app ?? Application.Current!),
                SingleViewTopLevelGroup gr => gr.Items.First(),
                _ => new Controls.TopLevelGroup(topLevelGroup)
            };

            // If single static toplevel is already visible in another devtools window, focus it.
            if (s_open.TryGetValue(topLevelGroup, out var mainWindow))
            {
                mainWindow.Activate();
                mainWindow.SelectedControl(focusedControl);
                return Disposable.Empty;
            }
            if (topLevelGroup.Items.Count == 1 && topLevelGroup.Items is not INotifyCollectionChanged)
            {
                var singleTopLevel = topLevelGroup.Items.First();
                
                foreach (var group in s_open)
                {
                    if (group.Key.Items.Contains(singleTopLevel))
                    {
                        group.Value.Activate();
                        group.Value.SelectedControl(focusedControl);
                        return Disposable.Empty;
                    }
                }
            }
            
            var window = new MainWindow
            {
                Root = root,
                Width = options.Size.Width,
                Height = options.Size.Height,
                Tag = topLevelGroup
            };
            window.SetOptions(options);
            window.SelectedControl(focusedControl);
            window.Closed += DevToolsClosed;
            s_open.Add(topLevelGroup, window);
            if (options.ShowAsChildWindow && owner is not null)
            {
                window.Show(owner);
            }
            else
            {
                window.Show();
            }
            return Disposable.Create(() => window.Close());
        }

        private static void DevToolsClosed(object? sender, EventArgs e)
        {
            var window = (MainWindow)sender!;
            window.Closed -= DevToolsClosed;
            s_open.Remove((IDevToolsTopLevelGroup)window.Tag!);
        }
    }
}
