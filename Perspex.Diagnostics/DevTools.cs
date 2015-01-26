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
    using Perspex.Diagnostics.ViewModels;
    using Perspex.Input;
    using ReactiveUI;

    public class DevTools : Decorator
    {
        public static readonly PerspexProperty<Control> RootProperty =
            PerspexProperty.Register<DevTools, Control>("Root");

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
                            new TreeDataTemplate<LogicalTreeNode>(GetHeader, x => x.Children),
                            new TreeDataTemplate<VisualTreeNode>(GetHeader, x => x.Children),
                        },
                        [!TreeView.ItemsProperty] = tabStrip.WhenAnyValue(x => x.SelectedTab.Tag),
                        [Grid.RowProperty] = 1,
                    })
                }
            };

            var detailsView = new ContentControl
            {
                DataTemplates = new DataTemplates
                {
                    new DataTemplate<ControlDetails>(CreateDetailsView),
                },
                [!ContentControl.ContentProperty] = treeView[!TreeView.SelectedItemProperty]
                    .Where(x => x != null)
                    .Cast<TreeNode>()
                    .Select(x => new ControlDetails(x.Control)),
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
            w.PreviewKeyDown += WindowPreviewKeyDown;
            return Disposable.Create(() => w.PreviewKeyDown -= WindowPreviewKeyDown);
        }

        private static void WindowPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F12)
            {
                Window window = new Window
                {
                    // HACK: Set width and height here as a quick fix as there's a problem with 
                    // the dev tools window hanging when it's set to auto-size.
                    Width = 800,
                    Height = 600,
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

        private static Control GetHeader(LogicalTreeNode node)
        {
            return new StackPanel
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
        }

        private static Control GetHeader(VisualTreeNode node)
        {
            return new StackPanel
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
        }
    }
}
