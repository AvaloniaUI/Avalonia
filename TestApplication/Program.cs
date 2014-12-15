using System.Reactive.Linq;
using Perspex;
using Perspex.Controls;
using Perspex.Controls.Primitives;
using Perspex.Diagnostics;
using Perspex.Layout;
using Perspex.Media;
using Perspex.Media.Imaging;
#if PERSPEX_GTK
using Perspex.Gtk;
#else
using Perspex.Win32;
#endif
using Splat;

namespace TestApplication
{
    class TestLogger : ILogger
    {
        public LogLevel Level
        {
            get;
            set;
        }

        public void Write(string message, LogLevel logLevel)
        {
            if ((int)logLevel < (int)Level) return;
            System.Diagnostics.Debug.WriteLine(message);
        }
    }

    class Item
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    class Node
    {
        public Node()
        {
            this.Children = new PerspexList<Node>();
        }

        public string Name { get; set; }
        public PerspexList<Node> Children { get; set; }
    }

    class Program
    {
        private static PerspexList<Node> treeData = new PerspexList<Node>
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

        private static PerspexList<Item> listBoxData = new PerspexList<Item>
        {
            new Item { Name = "Item 1", Value = "Item 1 Value" },
            new Item { Name = "Item 2", Value = "Item 2 Value" },
            new Item { Name = "Item 3", Value = "Item 3 Value" },
        };

        static void Main(string[] args)
        {
            //LogManager.Enable(new TestLogger());
            //LogManager.Instance.LogLayoutMessages = true;

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

            Window window = new Window
            {
                Title = "Perspex Test Application",
                Content = new Grid
                {
                    RowDefinitions = new RowDefinitions
                    {
                        new RowDefinition(1, GridUnitType.Star),
                        new RowDefinition(GridLength.Auto),
                    },
                    Children = new Controls
                    {
                        new TabControl
                        {
                            Items = new[]
                            {
                                ButtonsTab(),
                                TextTab(),
                                ImagesTab(),
                                ListsTab(),
                                SlidersTab(),
                                LayoutTab(),
                            }
                        },
                        new TextBlock
                        {
                            Text = "Press F12 for Dev Tools",
                            HorizontalAlignment = HorizontalAlignment.Right,
                            Margin = new Thickness(2),
                            [Grid.RowProperty] = 1,
                        }
                    }
                },
            };

            DevTools.Attach(window);

            window.Show();
            Application.Current.Run(window);
        }

        private static TabItem ButtonsTab()
        {
            return new TabItem
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
                        new Button
                        {
                            Content = "Button",
                        },
                        new Button
                        {
                            Content = "Button",
                            Background = new SolidColorBrush(0xcc119eda),
                        },
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
                        },
                        new TextBlock
                        {
                            Text = "Italic text.",
                            FontStyle = FontStyle.Italic,
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
                            Value = 400,
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
                                [!Image.WidthProperty] = size[!ScrollBar.ValueProperty],
                                [!Image.HeightProperty] = size[!ScrollBar.ValueProperty],
                            },
                        },
                    }
                },
            };
        }

        private static TabItem ListsTab()
        {
            return new TabItem
            {
                Header = "Lists",
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Gap = 8,
                    Children = new Controls
                    {
                        new TreeView
                        {
                            Id = "treeView",
                            Items = treeData,
                        },
                        new ListBox
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
                            Items = listBoxData,
                        }
                    }
                },
            };
        }

        private static TabItem SlidersTab()
        {
            ScrollBar sb;

            return new TabItem
            {
                Header = "Sliders",
                Content = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions
                    {
                        new ColumnDefinition(GridLength.Auto),
                        new ColumnDefinition(GridLength.Auto),
                    },
                    RowDefinitions = new RowDefinitions
                    {
                        new RowDefinition(GridLength.Auto),
                        new RowDefinition(GridLength.Auto),
                    },
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Children = new Controls
                    {
                        new ScrollBar
                        {
                            Orientation = Orientation.Vertical,
                            Value = 25,
                            Height = 300,
                            [Grid.ColumnProperty] = 0,
                            [Grid.RowProperty] = 1,
                        },
                        (sb = new ScrollBar
                        {
                            Orientation = Orientation.Horizontal,
                            ViewportSize = 25,
                            Value = 25,
                            Width = 300,
                            [Grid.ColumnProperty] = 1,
                            [Grid.RowProperty] = 0,
                        }),
                        new TextBlock
                        {
                            HorizontalAlignment = HorizontalAlignment.Center,
                            [!TextBlock.TextProperty] = sb[!ScrollBar.ValueProperty].Cast<double>().Select(x => x.ToString("0")),
                            [Grid.ColumnProperty] = 1,
                            [Grid.RowProperty] = 1,
                        }
                    },
                }
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
    }
}
