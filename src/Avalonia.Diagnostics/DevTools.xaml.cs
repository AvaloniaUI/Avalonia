using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Diagnostics.ViewModels;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

namespace Avalonia
{
	public static class DevToolsExtensions
	{
		public static void AttachDevTools(this TopLevel control)
		{
			Avalonia.Diagnostics.DevTools.Attach(control);
		}
	}
}

namespace Avalonia.Diagnostics
{
	public class DevTools : UserControl
    {
        private static Dictionary<TopLevel, Window> s_open = new Dictionary<TopLevel, Window>();
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

        public static IDisposable Attach(TopLevel control)
        {
            return control.AddHandler(
                KeyDownEvent,
                WindowPreviewKeyDown,
                RoutingStrategies.Tunnel);
        }

        private static void WindowPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F12)
            {
                var control = (TopLevel)sender;
                var devToolsWindow = default(Window);

                if (s_open.TryGetValue(control, out devToolsWindow))
                {
                    devToolsWindow.Activate();
                }
                else
                {
                    var devTools = new DevTools(control);

                    devToolsWindow = new Window
                    {
                        Width = 1024,
                        Height = 512,
                        Content = devTools,
                        DataTemplates =
                        {
                            new ViewLocator<ViewModelBase>(),
                        }
                    };

                    devToolsWindow.Closed += devTools.DevToolsClosed;
                    s_open.Add(control, devToolsWindow);
                    devToolsWindow.Show();
                }
            }
        }

        private void DevToolsClosed(object sender, EventArgs e)
        {
            var devToolsWindow = (Window)sender;
            var devTools = (DevTools)devToolsWindow.Content;
            s_open.Remove((TopLevel)devTools.Root);
            _keySubscription.Dispose();
            devToolsWindow.Closed -= DevToolsClosed;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void RawKeyDown(RawKeyEventArgs e)
        {
            const InputModifiers modifiers = InputModifiers.Control | InputModifiers.Shift;

            if ((e.Modifiers) == modifiers)
            {

                var point = (Root.VisualRoot as IInputRoot)?.MouseDevice?.GetPosition(Root) ?? default(Point);
                var control = Root.GetVisualsAt(point, x => (!(x is AdornerLayer) && x.IsVisible))
                    .FirstOrDefault();

                if (control != null)
                {
                    var vm = (DevToolsViewModel)DataContext;
                    vm.SelectControl((IControl)control);
                }
            }
        }
    }
}
