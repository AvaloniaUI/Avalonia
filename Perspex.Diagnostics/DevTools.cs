// -----------------------------------------------------------------------
// <copyright file="DevTools.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Diagnostics
{
    using System;
    using System.Reactive.Linq;
    using Perspex.Controls;
    using Perspex.Diagnostics.ViewModels;
    using Perspex.Input;
    using ReactiveUI;

    public class DevTools : Decorator
    {
        public static readonly PerspexProperty<Control> RootProperty =
            PerspexProperty.Register<DevTools, Control>("Root");

        private DevToolsViewModel viewModel;

        public DevTools()
        {
            this.viewModel = new DevToolsViewModel();
            this.GetObservable(RootProperty).Subscribe(x => this.viewModel.Root = x);

            this.InitializeComponent();
        }

        public Control Root
        {
            get { return this.GetValue(RootProperty); }
            set { this.SetValue(RootProperty, value); }
        }

        public static IDisposable Attach(Window window)
        {
            return window.AddHandler(
                Window.KeyDownEvent, 
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
            this.DataTemplates.Add(new ViewLocator<ReactiveObject>());

            this.Content = new Grid
            {
                RowDefinitions = new RowDefinitions
                {
                    new RowDefinition(new GridLength(1, GridUnitType.Star)),
                    new RowDefinition(GridLength.Auto),
                },
                Children = new Controls
                {
                    new TabControl
                    {
                        Items = new[]
                        {
                            new TabItem
                            {
                                Header = "Logical Tree",
                                [!TabItem.ContentProperty] = this.viewModel.WhenAnyValue(x => x.LogicalTree),
                            },
                            new TabItem
                            {
                                Header = "Visual Tree",
                                [!TabItem.ContentProperty] = this.viewModel.WhenAnyValue(x => x.VisualTree),
                            }
                        },
                    },
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Gap = 4,
                        [Grid.RowProperty] = 1,
                        Children = new Controls
                        {
                            new TextBlock
                            {
                                Text = "Focused: "
                            },
                            new TextBlock
                            {
                                [!TextBlock.TextProperty] = this.viewModel.WhenAnyValue(x => x.FocusedControl).Select(x => x?.GetType().Name)
                            }
                        }
                    }
                }
            };
        }
    }
}
