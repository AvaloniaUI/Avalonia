// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;
using Perspex.Controls;
using Perspex.Diagnostics.ViewModels;
using Perspex.Input;
using ReactiveUI;

namespace Perspex.Diagnostics
{
    public class DevTools : Decorator
    {
        public static readonly PerspexProperty<Control> RootProperty =
            PerspexProperty.Register<DevTools, Control>("Root");

        private DevToolsViewModel _viewModel;

        public DevTools()
        {
            _viewModel = new DevToolsViewModel();
            GetObservable(RootProperty).Subscribe(x => _viewModel.Root = x);

            InitializeComponent();
        }

        public Control Root
        {
            get { return GetValue(RootProperty); }
            set { SetValue(RootProperty, value); }
        }

        public static IDisposable Attach(Window window)
        {
            return window.AddHandler(
                KeyDownEvent,
                WindowPreviewKeyDown,
                Interactivity.RoutingStrategies.Tunnel);
        }

        private static void WindowPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F12)
            {
                Window window = new Window
                {
                    Width = 1024,
                    Height = 512,
                    Content = new DevTools
                    {
                        Root = (Window)sender,
                    },
                };

                window.Show();
            }
        }

        private void InitializeComponent()
        {
            DataTemplates.Add(new ViewLocator<ReactiveObject>());

            Child = new Grid
            {
                RowDefinitions = new RowDefinitions("*,Auto"),
                Children = new Controls.Controls
                {
                    new TabControl
                    {
                        Items = new[]
                        {
                            new TabItem
                            {
                                Header = "Logical Tree",
                                [!ContentControl.ContentProperty] = _viewModel.WhenAnyValue(x => x.LogicalTree),
                            },
                            new TabItem
                            {
                                Header = "Visual Tree",
                                [!ContentControl.ContentProperty] = _viewModel.WhenAnyValue(x => x.VisualTree),
                            }
                        },
                    },
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Gap = 4,
                        [Grid.RowProperty] = 1,
                        Children = new Controls.Controls
                        {
                            new TextBlock
                            {
                                Text = "Focused: "
                            },
                            new TextBlock
                            {
                                [!TextBlock.TextProperty] = _viewModel
                                    .WhenAnyValue(x => x.FocusedControl)
                                    .Select(x => x?.GetType().Name ?? "(null)")
                            },
                            new TextBlock
                            {
                                Text = "Pointer Over: "
                            },
                            new TextBlock
                            {
                                [!TextBlock.TextProperty] = _viewModel
                                    .WhenAnyValue(x => x.PointerOverElement)
                                    .Select(x => x?.GetType().Name ?? "(null)")
                            }
                        }
                    }
                }
            };
        }
    }
}
