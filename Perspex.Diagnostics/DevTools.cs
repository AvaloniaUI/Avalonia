// -----------------------------------------------------------------------
// <copyright file="DevTools.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Diagnostics
{
    using System;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using Perspex.Controls;
    using Perspex.Controls.Primitives;
    using Perspex.Controls.Shapes;
    using Perspex.Diagnostics.ViewModels;
    using Perspex.Input;
    using Perspex.Interactivity;
    using Perspex.Layout;
    using Perspex.Media;
    using ReactiveUI;

    public class DevTools : Decorator
    {
        public static readonly PerspexProperty<Control> RootProperty =
            PerspexProperty.Register<DevTools, Control>("Root");

        private Control adorner;

        public DevTools()
        {
            TabStrip tabStrip;
            TreeView treeView;

            var treePane = new Grid
            {
                RowDefinitions = new RowDefinitions
                {
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(new GridLength(1, GridUnitType.Star)),
                },
                Children = new Controls
                {
                    (tabStrip = new TabStrip
                    {
                        Items = new[]
                        {
                            new TabItem
                            {
                                Header = "Logical Tree",
                                IsSelected = true,
                                [!TabItem.TagProperty] = this[!RootProperty].Select(x => LogicalTreeNode.Create(x)),
                            },
                            new TabItem
                            {
                                Header = "Visual Tree",
                                [!TabItem.TagProperty] = this[!RootProperty].Select(x => VisualTreeNode.Create(x)),
                            }
                        },
                    }),
                    (treeView = new TreeView
                    {
                        DataTemplates = new DataTemplates
                        {
                            new TreeDataTemplate<LogicalTreeNode>(this.GetHeader, x => x.Children),
                            new TreeDataTemplate<VisualTreeNode>(this.GetHeader, x => x.Children),
                        },
                        [!TreeView.ItemsProperty] = tabStrip.WhenAnyValue(x => x.SelectedTab.Tag),
                        [Grid.RowProperty] = 1,
                    })
                }
            };

            var detailsView = new ScrollViewer
            {
                Content = new ContentControl
                {
                    DataTemplates = new DataTemplates
                    {
                        new DataTemplate<ControlDetails>(CreateDetailsView),
                    },
                    [!ContentControl.ContentProperty] = treeView[!TreeView.SelectedItemProperty]
                    .Where(x => x != null)
                    .Cast<TreeNode>()
                    .Select(x => new ControlDetails(x.Control)),
                },
                [Grid.ColumnProperty] = 2,
            };

            var splitter = new GridSplitter
            {
                [Grid.ColumnProperty] = 1,
                Width = 4,
            };

            this.Content = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions
                {
                    new ColumnDefinition(1, GridUnitType.Star),
                    new ColumnDefinition(4, GridUnitType.Pixel),
                    new ColumnDefinition(3, GridUnitType.Star),
                },
                Children = new Controls
                {
                    treePane,
                    splitter,
                    detailsView,
                }
            };
        }

        public Control Root
        {
            get { return this.GetValue(RootProperty); }
            set { this.SetValue(RootProperty, value); }
        }

        public static IDisposable Attach(Window w)
        {
            return w.AddHandler(Window.KeyDownEvent, WindowPreviewKeyDown, Interactivity.RoutingStrategies.Tunnel);
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

        private static Control CreateDetailsView(ControlDetails i)
        {
            return new ItemsControl
            {
                DataTemplates = new DataTemplates
                {
                    new DataTemplate<PropertyDetails>(x =>
                        new StackPanel
                        {
                            Gap = 16,
                            Orientation = Orientation.Horizontal,
                            Children = new Controls
                            {
                                new TextBlock { Text = x.Name },
                                new TextBlock { [!TextBlock.TextProperty] = x.WhenAnyValue(v => v.Value).Select(v => v.ToString()) },
                                new TextBlock { Text = x.Priority },
                            },
                        }),
                },
                Items = i.Properties,
            };
        }

        private Control GetHeader(LogicalTreeNode node)
        {
            var result = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Gap = 8,
                Children = new Controls
                {
                    new TextBlock
                    {
                        Text = node.Type,
                    },
                    new TextBlock
                    {
                        [!TextBlock.TextProperty] = node.WhenAnyValue(x => x.Classes),
                    }
                }
            };

            result.PointerEnter += this.AddAdorner;
            result.PointerLeave += this.RemoveAdorner;

            return result;
        }

        private Control GetHeader(VisualTreeNode node)
        {
            var result = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Gap = 8,
                Children = new Controls
                {
                    new TextBlock
                    {
                        Text = node.Type,
                        FontStyle = node.IsInTemplate ? Media.FontStyle.Italic : Media.FontStyle.Normal,
                    },
                    new TextBlock
                    {
                        [!TextBlock.TextProperty] = node.WhenAnyValue(x => x.Classes),
                    }
                }
            };

            result.PointerEnter += this.AddAdorner;
            result.PointerLeave += this.RemoveAdorner;

            return result;
        }

        private void AddAdorner(object sender, PointerEventArgs e)
        {
            var node = (TreeNode)((Control)sender).DataContext;
            var layer = AdornerLayer.GetAdornerLayer(node.Control);

            if (layer != null)
            {
                this.adorner = new Rectangle
                {
                    Fill = new SolidColorBrush(0x80a0c5e8),
                    [AdornerLayer.AdornedElementProperty] = node.Control,
                };

                layer.Children.Add(this.adorner);
            }
        }

        private void RemoveAdorner(object sender, PointerEventArgs e)
        {
            if (this.adorner != null)
            {
                ((Panel)this.adorner.Parent).Children.Remove(this.adorner);
                this.adorner = null;
            }
        }
    }
}
