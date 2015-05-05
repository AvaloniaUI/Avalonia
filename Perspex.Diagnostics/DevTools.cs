// -----------------------------------------------------------------------
// <copyright file="DevTools.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Diagnostics
{
    using Perspex.Controls;
    using Perspex.Diagnostics.ViewModels;
    using Perspex.Input;
    using ReactiveUI;
    using System;
    using System.Reactive.Linq;

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

            this.Content = new TabControl
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
            };
        }

        //private Control GetHeader(VisualTreeNode node)
        //{
        //    var result = new StackPanel
        //    {
        //        Orientation = Orientation.Horizontal,
        //        Gap = 8,
        //        Children = new Controls
        //        {
        //            new TextBlock
        //            {
        //                Text = node.Type,
        //                FontStyle = node.IsInTemplate ? Media.FontStyle.Italic : Media.FontStyle.Normal,
        //            },
        //            new TextBlock
        //            {
        //                [!TextBlock.TextProperty] = node.WhenAnyValue(x => x.Classes),
        //            }
        //        }
        //    };

        //    result.PointerEnter += this.AddAdorner;
        //    result.PointerLeave += this.RemoveAdorner;

        //    return result;
        //}
    }
}
