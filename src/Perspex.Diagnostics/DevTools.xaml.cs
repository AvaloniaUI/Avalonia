using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Perspex.Controls;
using Perspex.Controls.Primitives;
using Perspex.Controls.Templates;
using Perspex.Diagnostics.ViewModels;
using Perspex.Input;
using Perspex.Input.Raw;
using Perspex.Interactivity;
using Perspex.Markup.Xaml;
using Perspex.VisualTree;
using ReactiveUI;

namespace Perspex
{
	public static class WindowExtensions
	{
		public static void AttachDevTools(this Window window)
		{
			Perspex.Diagnostics.DevTools.Attach(window);
		}
	}
}

namespace Perspex.Diagnostics
{
	public class DevTools : UserControl
    {
        private static Dictionary<Window, Window> s_open = new Dictionary<Window, Window>();
        private IDisposable _keySubscription;

        public DevTools(IControl root)
        {
            InitializeComponent();
            Root = root;
            DataContext = new DevToolsViewModel(root);

            _keySubscription = InputManager.Instance.Process
                .OfType<RawKeyEventArgs>()
                .Subscribe(RawKeyDown);
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
                    var devTools = new DevTools(window);

                    devToolsWindow = new Window
                    {
                        Width = 1024,
                        Height = 512,
                        Content = devTools,
                        DataTemplates = new DataTemplates
                        {
                            new ViewLocator<ReactiveObject>(),
                        }
                    };

                    devToolsWindow.Closed += devTools.DevToolsClosed;
                    s_open.Add((Window)sender, devToolsWindow);
                    devToolsWindow.Show();
                }
            }
        }

        private void DevToolsClosed(object sender, EventArgs e)
        {
            var devToolsWindow = (Window)sender;
            var devTools = (DevTools)devToolsWindow.Content;
            var window = (Window)devTools.Root;

            s_open.Remove(window);
            _keySubscription.Dispose();
            devToolsWindow.Closed -= DevToolsClosed;
        }

        private void InitializeComponent()
        {
            PerspexXamlLoader.Load(this);
        }

        private void RawKeyDown(RawKeyEventArgs e)
        {
            const InputModifiers modifiers = InputModifiers.Control | InputModifiers.Shift;

            if ((e.Modifiers) == modifiers)
            {
                var point = MouseDevice.Instance.GetPosition(Root);
                var control = Root.GetVisualsAt(point).FirstOrDefault(x => !(x is AdornerLayer));

                if (control != null)
                {
                    var vm = (DevToolsViewModel)DataContext;
                    vm.SelectControl((IControl)control);
                }
            }
        }
    }
}
