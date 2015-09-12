﻿// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.IO;
using System.Reactive.Linq;
using Perspex;
using Perspex.Animation;
using Perspex.Collections;
using Perspex.Controls;
using Perspex.Controls.Html;
using Perspex.Controls.Primitives;
using Perspex.Controls.Shapes;
using Perspex.Controls.Templates;
using Perspex.Diagnostics;
using Perspex.Layout;
using Perspex.Media;
using Perspex.Media.Imaging;
#if PERSPEX_GTK
using Perspex.Gtk;
#endif
using ReactiveUI;

namespace TestApplication
{
    internal class Item
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    internal class Node
    {
        public Node()
        {
            Children = new PerspexList<Node>();
        }

        public string Name { get; set; }
        public PerspexList<Node> Children { get; set; }
    }

    internal class Program
    {
        private static readonly PerspexList<Node> s_treeData = new PerspexList<Node>
        {
            new Node
            {
                Name = "Root 1",
                Children = new PerspexList<Node>
                {
                    new Node
                    {
                        Name = "Child 1",
                    },
                    new Node
                    {
                        Name = "Child 2",
                        Children = new PerspexList<Node>
                        {
                            new Node
                            {
                                Name = "Grandchild 1",
                            },
                            new Node
                            {
                                Name = "Grandmaster Flash",
                            },
                        }
                    },
                    new Node
                    {
                        Name = "Child 3",
                    },
                }
            },
            new Node
            {
                Name = "Root 2",
            },
        };

        private static readonly PerspexList<Item> s_listBoxData = new PerspexList<Item>
        {
            new Item { Name = "Item 1", Value = "Item 1 Value" },
            new Item { Name = "Item 2", Value = "Item 2 Value" },
            new Item { Name = "Item 3", Value = "Item 3 Value" },
            new Item { Name = "Item 4", Value = "Item 4 Value" },
            new Item { Name = "Item 5", Value = "Item 5 Value" },
            new Item { Name = "Item 6", Value = "Item 6 Value" },
            new Item { Name = "Item 7", Value = "Item 7 Value" },
            new Item { Name = "Item 8", Value = "Item 8 Value" },
        };

        private static void Main(string[] args)
        {
            //Log.Logger = new LoggerConfiguration()
            //    .Filter.ByIncludingOnly(Matching.WithProperty("Area", "Layout"))
            //    .MinimumLevel.Verbose()
            //    .WriteTo.Trace(outputTemplate: "[{Id:X8}] [{SourceContext}] {Message}")
            //    .CreateLogger();

            // The version of ReactiveUI currently included is for WPF and so expects a WPF
            // dispatcher. This makes sure it's initialized.
            System.Windows.Threading.Dispatcher foo = System.Windows.Threading.Dispatcher.CurrentDispatcher;

            App application = new App
            {
                DataTemplates = new DataTemplates
                {
                    new TreeDataTemplate<Node>(
                        x => new TextBlock { Text = x.Name },
                        x => x.Children,
                        x => true),
                },
            };

            TextBlock fps;

            var testCommand = ReactiveCommand.Create();
            testCommand.Subscribe(_ => System.Diagnostics.Debug.WriteLine("Test command executed."));

            Window window = new Window
            {
                Title = "Perspex Test Application",
                SizeToContent = SizeToContent.WidthAndHeight,
                Content = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions
                    {
                        new ColumnDefinition(1, GridUnitType.Star),
                        new ColumnDefinition(1, GridUnitType.Star),
                    },
                    RowDefinitions = new RowDefinitions
                    {
                        new RowDefinition(GridLength.Auto),
                        new RowDefinition(1, GridUnitType.Star),
                        new RowDefinition(GridLength.Auto),
                    },
                    Children = new Controls
                    {
                        new Menu
                        {
                            Items = new[]
                            {
                                new MenuItem
                                {
                                    Header = "_File",
                                    Items = new[]
                                    {
                                        new MenuItem
                                        {
                                            Header = "_Open...",
                                            Icon = new Image
                                            {
                                                Source = new Bitmap("github_icon.png"),
                                            },
                                        },
                                        new MenuItem
                                        {
                                            Header = "_Save",
                                            Items = new[]
                                            {
                                                new MenuItem
                                                {
                                                    Header = "Sub Item _1",
                                                },
                                                new MenuItem
                                                {
                                                    Header = "Sub Item _2",
                                                },
                                            }
                                        },
                                        new MenuItem
                                        {
                                            Header = "Save _As",
                                            Items = new[]
                                            {
                                                new MenuItem
                                                {
                                                    Header = "Sub Item _1",
                                                },
                                                new MenuItem
                                                {
                                                    Header = "Sub Item _2",
                                                },
                                            }
                                        },
                                        new MenuItem
                                        {
                                            Header = "E_xit",
                                            Command = testCommand,
                                        },
                                    }
                                },
                                new MenuItem
                                {
                                    Header = "_Edit",
                                    Items = new[]
                                    {
                                        new MenuItem
                                        {
                                            Header = "Cu_t",
                                        },
                                        new MenuItem
                                        {
                                            Header = "_Copy",
                                        },
                                        new MenuItem
                                        {
                                            Header = "_Paste",
                                        },
                                    }
                                }
                            },
                            [Grid.ColumnSpanProperty] = 2,
                        },
                        new TabControl
                        {
                            Items = new[]
                            {
                                ButtonsTab(),
                                TextTab(),
                                HtmlTab(),
                                ImagesTab(),
                                ListsTab(),
                                LayoutTab(),
                                AnimationsTab(),
                            },
                            Transition = new PageSlide(TimeSpan.FromSeconds(0.25)),
                            [Grid.RowProperty] = 1,
                            [Grid.ColumnSpanProperty] = 2,
                        },
                        (fps = new TextBlock
                        {
                            HorizontalAlignment = HorizontalAlignment.Left,
                            Margin = new Thickness(2),
                            [Grid.RowProperty] = 2,
                        }),
                        new TextBlock
                        {
                            Text = "Press F12 for Dev Tools",
                            HorizontalAlignment = HorizontalAlignment.Right,
                            Margin = new Thickness(2),
                            [Grid.ColumnProperty] = 1,
                            [Grid.RowProperty] = 2,
                        },
                    }
                },
            };

            DevTools.Attach(window);

            //var renderer = ((IRenderRoot)window).Renderer;
            //var last = renderer.RenderCount;
            //DispatcherTimer.Run(() =>
            //{
            //    fps.Text = "FPS: " + (renderer.RenderCount - last);
            //    last = renderer.RenderCount;
            //    return true;
            //}, TimeSpan.FromSeconds(1));

            window.Show();
            Application.Current.Run(window);
        }

        private static TabItem ButtonsTab()
        {
            Button defaultButton;

            var showDialog = ReactiveCommand.Create();
            Button showDialogButton;

            var result = new TabItem
            {
                Header = "Buttons",
                Content = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Gap = 8,
                    MinWidth = 120,
                    Children = new Controls
                    {
                        (showDialogButton = new Button
                        {
                            Content = "Button",
                            Command = showDialog,
                            [ToolTip.TipProperty] = "Hello World!",
                        }),
                        new Button
                        {
                            Content = "Button",
                            Background = new SolidColorBrush(0xcc119eda),
                            [ToolTip.TipProperty] = "Goodbye Cruel World!",
                        },
                        (defaultButton = new Button
                        {
                            Content = "Default",
                            IsDefault = true,
                        }),
                        new Button
                        {
                            Content = "Disabled",
                            IsEnabled = false,
                        },
                        new Button
                        {
                            Content = "Disabled",
                            IsEnabled = false,
                            Background = new SolidColorBrush(0xcc119eda),
                        },
                        new ToggleButton
                        {
                            Content = "Toggle",
                        },
                        new ToggleButton
                        {
                            Content = "Disabled",
                            IsEnabled = false,
                        },
                        new CheckBox
                        {
                            Content = "Checkbox",
                        },
                        new RadioButton
                        {
                            Content = "RadioButton 1",
                            IsChecked = true,
                        },
                        new RadioButton
                        {
                            Content = "RadioButton 2",
                        },
                    }
                },
            };

            defaultButton.Click += (s, e) =>
            {
                defaultButton.Content = ((string)defaultButton.Content == "Default") ? "Clicked" : "Default";
            };

            showDialog.Subscribe(async _ =>
            {
                var close = ReactiveCommand.Create();

                var dialog = new Window
                {
                    Content = new StackPanel
                    {
                        Width = 200,
                        Height = 200,
                        Children = new Controls
                        {
                            new Button { Content = "Yes", Command = close, CommandParameter = "Yes" },
                            new Button { Content = "No", Command = close, CommandParameter = "No" },
                        }
                    }
                };

                close.Subscribe(x => dialog.Close(x));

                showDialogButton.Content = await dialog.ShowDialog<string>();
            });

            return result;
        }

        private static TabItem HtmlTab()
        {
            var htmlText =
                new StreamReader(typeof (Program).Assembly.GetManifestResourceStream("TestApplication.html.htm"))
                    .ReadToEnd();
            return new TabItem
            {
                Header = "Html",
                Content = new ScrollViewer()
                {
                    Width = 600,
                    MaxHeight = 600,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    CanScrollHorizontally = false,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
                    Content =
                        new HtmlLabel()
                        {
                            Text = htmlText
                        }
                }
            };
        }

        private static TabItem TextTab()
        {
            return new TabItem
            {
                Header = "Text",
                Content = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Gap = 8,
                    Width = 120,
                    Children = new Controls
                    {
                        new TextBlock
                        {
                            Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Proin venenatis dui quis libero suscipit tincidunt.",
                            TextWrapping = TextWrapping.Wrap,
                            TextAlignment = TextAlignment.Center,
                        },
                        new TextBlock
                        {
                            Text = "Italic text.",
                            FontStyle = FontStyle.Italic,
                            TextAlignment = TextAlignment.Left,
                        },
                        new TextBlock
                        {
                            Text = "Bold text.",
                            FontWeight = FontWeight.Bold,
                            TextAlignment = TextAlignment.Right,
                        },
                        new TextBox
                        {
                            Text = "A non-wrapping text box. Lorem ipsum dolor sit amet.",
                            TextWrapping = TextWrapping.NoWrap,
                        },
                        new TextBox
                        {
                            AcceptsReturn = true,
                            Text = "A wrapping text box. " +
                                   "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Proin venenatis dui quis libero suscipit tincidunt. " +
                                   "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Proin venenatis dui quis libero suscipit tincidunt.",
                            TextWrapping = TextWrapping.Wrap,
                            MaxHeight = 100,
                        },
                    }
                },
            };
        }

        private static TabItem ImagesTab()
        {
            ScrollBar size;

            return new TabItem
            {
                Header = "Images",
                Content = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Gap = 8,
                    Children = new Controls
                    {
                        (size = new ScrollBar
                        {
                            Minimum = 100,
                            Maximum = 400,
                            Value = 100,
                            Orientation = Orientation.Horizontal,
                        }),
                        new ScrollViewer
                        {
                            Width = 200,
                            Height = 200,
                            CanScrollHorizontally = true,
                            Content = new Image
                            {
                                Source = new Bitmap("github_icon.png"),
                                [!Layoutable.WidthProperty] = size[!RangeBase.ValueProperty],
                                [!Layoutable.HeightProperty] = size[!RangeBase.ValueProperty],
                            },
                        },
                        new ProgressBar
                        {
                            [!RangeBase.MinimumProperty] = size[!RangeBase.MinimumProperty],
                            [!RangeBase.MaximumProperty] = size[!RangeBase.MaximumProperty],
                            [!RangeBase.ValueProperty] = size[!RangeBase.ValueProperty],
                        }
                    }
                },
            };
        }

        private static TabItem ListsTab()
        {
            ListBox listBox;

            return new TabItem
            {
                Header = "Lists",
                Content = new StackPanel
                {
                    DataTemplates = new DataTemplates
                    {
                        new DataTemplate<Item>(x =>
                            new StackPanel
                            {
                                Children = new Controls
                                {
                                    new TextBlock { Text = x.Name, FontSize = 24 },
                                    new TextBlock { Text = x.Value },
                                }
                            })
                    },
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Gap = 8,
                    Children = new Controls
                    {
                        new TreeView
                        {
                            Name = "treeView",
                            Items = s_treeData,
                        },
                        (listBox = new ListBox
                        {
                            Items = s_listBoxData,
                            MaxHeight = 300,
                        }),
                        new DropDown
                        {
                            Items = s_listBoxData,
                            SelectedItem = s_listBoxData[0],
                            VerticalAlignment = VerticalAlignment.Center,
                        }
                    }
                },
            };
        }

        private static TabItem LayoutTab()
        {
            return new TabItem
            {
                Header = "Layout",
                Content = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions
                    {
                        new ColumnDefinition(1, GridUnitType.Star),
                        new ColumnDefinition(1, GridUnitType.Star),
                    },
                    Margin = new Thickness(50),
                    Children = new Controls
                    {
                        new StackPanel
                        {
                            Orientation = Orientation.Vertical,
                            Gap = 8,
                            Children = new Controls
                            {
                                new Button { HorizontalAlignment = HorizontalAlignment.Left, Content = "Left Aligned" },
                                new Button { HorizontalAlignment = HorizontalAlignment.Center, Content = "Center Aligned" },
                                new Button { HorizontalAlignment = HorizontalAlignment.Right, Content = "Right Aligned" },
                                new Button { HorizontalAlignment = HorizontalAlignment.Stretch, Content = "Stretch" },
                            },
                            [Grid.ColumnProperty] = 0,
                        },
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Gap = 8,
                            Children = new Controls
                            {
                                new Button { VerticalAlignment = VerticalAlignment.Top, Content = "Top Aligned" },
                                new Button { VerticalAlignment = VerticalAlignment.Center, Content = "Center Aligned" },
                                new Button { VerticalAlignment = VerticalAlignment.Bottom, Content = "Bottom Aligned" },
                                new Button { VerticalAlignment = VerticalAlignment.Stretch, Content = "Stretch" },
                            },
                            [Grid.ColumnProperty] = 1,
                        },
                    },
                }
            };
        }

        private static TabItem AnimationsTab()
        {
            Border border1;
            Border border2;
            RotateTransform rotate;
            Button button1;

            var result = new TabItem
            {
                Header = "Animations",
                Content = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions
                    {
                        new ColumnDefinition(1, GridUnitType.Star),
                        new ColumnDefinition(1, GridUnitType.Star),
                    },
                    RowDefinitions = new RowDefinitions
                    {
                        new RowDefinition(1, GridUnitType.Star),
                        new RowDefinition(GridLength.Auto),
                    },
                    Children = new Controls
                    {
                        (border1 = new Border
                        {
                            Width = 100,
                            Height = 100,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            Background = Brushes.Crimson,
                            RenderTransform = new RotateTransform(),
                            Child = new TextBox
                            {
                                Background = Brushes.White,
                                Text = "Hello!",
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                            },
                        }),
                        (border2 = new Border
                        {
                            Width = 100,
                            Height = 100,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            Background = Brushes.Coral,
                            Child = new Image
                            {
                                Source = new Bitmap("github_icon.png"),
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                            },
                            RenderTransform = (rotate = new RotateTransform
                            {
                                PropertyTransitions = new PropertyTransitions
                                {
                                    RotateTransform.AngleProperty.Transition(500),
                                }
                            }),
                            PropertyTransitions = new PropertyTransitions
                            {
                                Layoutable.WidthProperty.Transition(300),
                                Layoutable.HeightProperty.Transition(1000),
                            },
                            [Grid.ColumnProperty] = 1,
                        }),
                        (button1 = new Button
                        {
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Content = "Animate",
                            [Grid.ColumnProperty] = 1,
                            [Grid.RowProperty] = 1,
                        }),
                    },
                },
            };

            button1.Click += (s, e) =>
            {
                if (border2.Width == 100)
                {
                    border2.Width = border2.Height = 400;
                    rotate.Angle = 180;
                }
                else
                {
                    border2.Width = border2.Height = 100;
                    rotate.Angle = 0;
                }
            };

            var start = Animate.Stopwatch.Elapsed;
            var degrees = Animate.Timer
                .Select(x =>
                {
                    var elapsed = (x - start).TotalSeconds;
                    var cycles = elapsed / 4;
                    var progress = cycles % 1;
                    return 360.0 * progress;
                });

            border1.RenderTransform.Bind(
                RotateTransform.AngleProperty,
                degrees,
                BindingPriority.Animation);

            return result;
        }
    }
}
