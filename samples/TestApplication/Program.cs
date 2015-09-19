// Copyright (c) The Perspex Project. All rights reserved.
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

            TabControl container;

            Window window = new Window
            {
                Title = "Perspex Test Application",
                Width = 800,
                Height = 300,
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
                        (container = new TabControl
                        {
                            Padding = new Thickness(5),
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
                            Transition = new CrossFade(TimeSpan.FromSeconds(0.25)),
                            [Grid.RowProperty] = 1,
                            [Grid.ColumnSpanProperty] = 2,
                        })
                    }
                },
            };

            container.Classes.Add(":container");

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
                Header = "Button",
                Content = new StackPanel
                {
                    Margin = new Thickness(10),
                    Orientation = Orientation.Vertical,
                    Gap = 4,
                    Children = new Controls
                    {
                        new TextBlock
                        {
                            Text = "Button",
                            FontWeight = FontWeight.Medium,
                            FontSize = 22,
                            Foreground = SolidColorBrush.Parse("#212121"),
                        },
                        new TextBlock
                        {
                            Text = "A button control",
                            FontSize = 14,
                            Foreground = SolidColorBrush.Parse("#727272"),
                            Margin = new Thickness(0, 0, 0, 10)
                        },
                        new Button
                        {
                            Width = 150,
                            Content = "Button"
                        },
                        new Button
                        {
                            Width   = 150,
                            Content = "Disabled",
                            IsEnabled = false,
                        },
                        new TabControl
                        {
                            Margin = new Thickness(0, 20, 0, 0),

                            Items = new []
                            {
                                new TabItem
                                {
                                    Header = new TextBlock { FontWeight = FontWeight.Medium, Text = "CSHARP" },
                                    Content = new HtmlLabel
                                    {
                                        Text = "CSHRP CODEZ"
                                    }
                                }, 
                                new TabItem
                                {
                                    Header = new TextBlock { FontWeight = FontWeight.Medium, Text = "XAML" },
                                    Content = new HtmlLabel
                                    {
                                        Text = "XAML CODEZ"
                                    }
                                }
                            }
                        }
                    }
                },
            };
            

            return result;
        }

        private static TabItem HtmlTab()
        {
            var htmlText =
                new StreamReader(typeof (Program).Assembly.GetManifestResourceStream("TestApplication.html.htm"))
                    .ReadToEnd();
            return new TabItem
            {
                Header = "HTML Label",
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
                Header = "Input",
                Content = new StackPanel
                {
                    Margin = new Thickness(10),
                    Orientation = Orientation.Vertical,
                    Gap = 4,
                    Children = new Controls
                    {
                        new TextBlock
                        {
                            Text = "Check box",
                            FontWeight = FontWeight.Medium,
                            FontSize = 22,
                            Foreground = SolidColorBrush.Parse("#212121"),
                        },
                        new TextBlock
                        {
                            Text = "A check box control",
                            FontSize = 14,
                            Foreground = SolidColorBrush.Parse("#373749"),
                            Margin = new Thickness(0, 0, 0, 10)
                        },
                        new CheckBox { IsChecked = true, Margin = new Thickness(0, 0, 0, 5), Content = "Checked" },
                        new CheckBox { IsChecked = false, Content = "Unchecked" },
                        new TabControl
                        {
                            Margin = new Thickness(0, 20, 0, 0),

                            Items = new []
                            {
                                new TabItem
                                {
                                    Header = new TextBlock { FontWeight = FontWeight.Medium, Text = "CSHARP" },
                                    Content = new HtmlLabel
                                    {
                                        Text = "CSHRP CODEZ"
                                    }
                                },
                                new TabItem
                                {
                                    Header = new TextBlock { FontWeight = FontWeight.Medium, Text = "XAML" },
                                    Content = new HtmlLabel
                                    {
                                        Text = "XAML CODEZ"
                                    }
                                }
                            }
                        },
                        new TextBlock
                        {
                            Margin = new Thickness(0, 40, 0, 0),
                            Text = "Radio button",
                            FontWeight = FontWeight.Medium,
                            FontSize = 22,
                            Foreground = SolidColorBrush.Parse("#373749"),
                        },
                        new TextBlock
                        {
                            Text = "A radio button control",
                            FontSize = 14,
                            Foreground = SolidColorBrush.Parse("#373749"),
                            Margin = new Thickness(0, 0, 0, 10)
                        },

                        new RadioButton { IsChecked = true, Margin = new Thickness(0, 0, 0, 5), Content = "Option 1" },
                        new RadioButton { IsChecked = false, Content = "Option 2" },
                        new RadioButton { IsChecked = false, Content = "Option 3" },
                        new TabControl
                        {
                            Margin = new Thickness(0, 20, 0, 0),

                            Items = new []
                            {
                                new TabItem
                                {
                                    Header = new TextBlock { FontWeight = FontWeight.Medium, Text = "CSHARP" },
                                    Content = new HtmlLabel
                                    {
                                        Text = "CSHRP CODEZ"
                                    }
                                },
                                new TabItem
                                {
                                    Header = new TextBlock { FontWeight = FontWeight.Medium, Text = "XAML" },
                                    Content = new HtmlLabel
                                    {
                                        Text = "XAML CODEZ"
                                    }
                                }
                            }
                        }
                    }
                }
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
