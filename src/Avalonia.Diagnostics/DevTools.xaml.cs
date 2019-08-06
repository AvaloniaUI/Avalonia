// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Diagnostics.ViewModels;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Rendering;
using Avalonia.VisualTree;

namespace Avalonia
{
    public static class DevToolsExtensions
    {
        public static void AttachDevTools(this TopLevel control)
        {
            Diagnostics.DevTools.Attach(control);
        }
    }
}

namespace Avalonia.Diagnostics
{
    public class DevTools : UserControl
    {
        private static readonly Dictionary<TopLevel, Window> s_open = new Dictionary<TopLevel, Window>();
        private static readonly HashSet<IRenderRoot> s_visualTreeRoots = new HashSet<IRenderRoot>();
        private readonly IDisposable _keySubscription;

        public DevTools(IControl root)
        {
            InitializeComponent();
            Root = root;
            DataContext = new DevToolsViewModel(root);

            _keySubscription = InputManager.Instance.Process
                .OfType<RawKeyEventArgs>()
                .Subscribe(RawKeyDown);
        }

        // HACK: needed for XAMLIL, will fix that later
        public DevTools()
        {
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

                if (s_open.TryGetValue(control, out var devToolsWindow))
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
                        DataTemplates = { new ViewLocator<ViewModelBase>() },
                        Title = "Avalonia DevTools"
                    };

                    devToolsWindow.Closed += devTools.DevToolsClosed;
                    s_open.Add(control, devToolsWindow);
                    MarkAsDevTool(devToolsWindow);
                    devToolsWindow.Show();
                }
            }
        }

        private void DevToolsClosed(object sender, EventArgs e)
        {
            var devToolsWindow = (Window)sender;
            var devTools = (DevTools)devToolsWindow.Content;
            s_open.Remove((TopLevel)devTools.Root);
            RemoveDevTool(devToolsWindow);
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

        /// <summary>
        /// Marks a visual as part of the DevTools, so it can be excluded from event tracking.
        /// </summary>
        /// <param name="visual">The visual whose root is to be marked.</param>
        public static void MarkAsDevTool(IVisual visual)
        {
            s_visualTreeRoots.Add(visual.GetVisualRoot());
        }

        public static void RemoveDevTool(IVisual visual)
        {
            s_visualTreeRoots.Remove(visual.GetVisualRoot());
        }

        public static bool BelongsToDevTool(IVisual visual)
        {
            return s_visualTreeRoots.Contains(visual.GetVisualRoot());
        }
    }
}
